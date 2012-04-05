\ lisp.fs --- A straighforward dynamically-scoped Lisp interpreter

\ Copyright 2012 (C) David Vazquez

\ This file is part of Eulex.

\ Eulex is free software: you can redistribute it and/or modify
\ it under the terms of the GNU General Public License as published by
\ the Free Software Foundation, either version 3 of the License, or
\ (at your option) any later version.

\ Eulex is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with Eulex.  If not, see <http://www.gnu.org/licenses/>.

vocabulary lisp
get-current
also eulex
also lisp definitions

\ : DEBUG ;

' postpone alias `` immediate
' compile-only alias c/o
: imm-c/o immediate compile-only ;

3 constant tag-bits
1 tag-bits lshift 1 - constant tag-mask

%000 constant even-fixnum-tag
%001 constant cons-tag
%010 constant symbol-tag
%011 constant subr-tag
%100 constant odd-fixnum-tag
\ %101 constant reserved
\ %110 constant reserved
%111 constant forward-tag

: tag tag-mask and ;
: tagged or ;
: ?tagged swap dup 0= if nip else swap tagged then ;
: untag tag-mask invert and ;

\ Memory management and garbage collection
\ ( Cheney's algorithm )

\ A stack of pinned objects. Use it to protect shadown symbol values
\ and other Lisp objects which could not to be accesible from Lisp
\ temporarly.
4096 constant pinned-size
create pinned pinned-size zallot
variable pinned-count

: >p
    pinned-count @ pinned-size = if abort" Too depth recursion." endif
    pinned pinned-count @ cells + ! pinned-count 1+! ;
: pdrop pinned-count 1-! ;

: pinargs ( arg1 ... argn n -- arg1 ... argn n )
    dup 0 ?do i 1+ pick >p loop ;

: unpinargs ( n -- )
    0 ?do pdrop loop ;

65536 constant dynamic-space

: initvar variable latestxt execute ! ;

\ From space
dynamic-space allocate throw  initvar fromsp-base
fromsp-base @ dynamic-space + initvar fromsp-limit
\ To space
dynamic-space allocate throw  initvar tosp-base
tosp-base @ dynamic-space +   initvar tosp-limit

variable &alloc
variable &scan

\ These functions receive a Lisp object and return a reference to the
\ new object in the to space. It allocates the object if it is
\ required, marking it with a forward pointer.
create copy-functions 1 tag-bits lshift cells zallot

: copy-method! ( xt tag -- )
    cells copy-functions + ! ;
: copy-method ( obj -- xt )
    tag cells copy-functions + @ ;

: swap-cells ( addr1 addr2 -- )
    dup @ -rot >r dup @ r> ! ! ;

: swap-spaces
    fromsp-base tosp-base swap-cells
    fromsp-limit tosp-limit swap-cells
    tosp-base @ dup &alloc ! &scan ! ; latestxt execute

: >tosp ( addr n -- addr* )
    tuck &alloc @ swap move
    &alloc @ swap &alloc +! ;

: forward-reference ( old -- new|0 )
    dup tag swap untag @ ( tag cell )
    dup tag forward-tag = if untag tagged else 2drop 0 endif ;

: markgc! ( old new -- )
    untag forward-tag tagged swap untag ! ;

: valid-obj? ( obj -- )
    fromsp-base @ swap untag fromsp-limit @ between ;
    
: copy ( x -- x* )
    dup copy-method over valid-obj? and if
        dup forward-reference ?dup if
        else
            dup dup copy-method execute tuck markgc!
        endif
    endif ;

defer copy-root-symbols

: copy-pinned
    pinned-count @ 0 ?do
        pinned i cells +
        dup @ copy swap !
    loop ;

: gc
    [IFDEF] DEBUG CR ." Garbage collecting..." [ENDIF]
    swap-spaces
    copy-root-symbols copy-pinned
    begin
    &scan @ &alloc @ u< while
        &scan @ dup @ copy swap !
        &scan @ cell+ &scan !
    repeat ;

: alloc-obj ( n -- obj f )
    dup &alloc @ + tosp-limit @ >= if gc endif
    dup &alloc @ + tosp-limit @ >= if abort" Out of memory" endif
    &alloc @ swap &alloc +! 0 ;

[IFDEF] DEBUG
: .debug
    CR
    ." From space: "
    fromsp-base @ dynamic-space dump CR
    ." ALLOC = " &alloc @ print-hex-number CR
    ." SCAN  = " &scan @ print-hex-number CR
    ." To space: "
    tosp-base @ dynamic-space dump CR ;
[ENDIF]



\ Errors
: void-variable 1 throw ;
: void-function 2 throw ;
: wrong-type-argument 3 throw ;
: wrong-number-of-arguments 4 throw ;
: parse-error 5 throw ;
: quit-condition 6 throw ;
: eof-condition 7 throw ;

\ Defered words
defer eval-lisp-obj
defer read-lisp-obj

\ Symbols

\ We write the lisp package system upon wordlists. The PF of the words
\ contains the symbol value and the symbol function parameters aligned
\ to a double cell size.
wordlist constant lisp-package

: in-lisp-package:
    lisp-package 1 set-order ;

: create-in-lisp-package
    get-order get-current in-lisp-package: definitions
    create set-current set-order ;

: find-cname-in-lisp-package ( c-addr -- nt|0 )
    >r get-order in-lisp-package: r>
    find-cname >r set-order r> ;

: create-symbol
    create-in-lisp-package latest 2align , does> 2aligned symbol-tag tagged ;

: ::unbound [ here 2aligned symbol-tag tagged ]L ;

create-symbol t   latestxt execute , ::unbound ,
create-symbol nil latestxt execute , ::unbound ,
create-symbol quote  ::unbound , ::unbound ,
create-symbol progn  ::unbound , ::unbound ,
create-symbol lambda ::unbound , ::unbound ,
create-symbol macro  ::unbound , ::unbound ,
create-symbol if     ::unbound , ::unbound ,

create-symbol backquote ::unbound , ::unbound ,
create-symbol comma     ::unbound , ::unbound ,

    
: find-symbol ( c-addr -- symbol|0 )
    find-cname-in-lisp-package dup if nt>xt execute endif ;

: intern-symbol ( c-addr -- symbol )
    dup find-symbol ?dup if nip else
        count nextname create-symbol ::unbound , ::unbound ,
        latestxt execute 
    then ;

: '' parse-cname find-symbol ;
: [''] '' `` literal ; immediate

\ #DOSYMBOLS...#ENDSYMBOLS
\ 
\ Iterate across the symbols in the package. The body is executed with
\ a symbol in the TOS each time. The body must drop the symbol from
\ the stack.
: #dosymbols
    `` lisp-package
    `` DOWORDS
        `` dup `` >r
        `` nt>xt `` execute
; imm-c/o

: #endsymbols
    `` r>
    `` ENDWORDS
; imm-c/o
 
'' t constant t
'' nil constant nil

: >bool if t else nil then ;
: bool> nil = if 0 else -1 then ;

: #symbolp tag symbol-tag = >bool ;

\ Check if X is a symbol object. If not, it signals an error.
: check-symbol ( x -- x )
    dup #symbolp nil = if wrong-type-argument then ;

: symbol-name ( symbol -- caddr )
    check-symbol untag @ ;

: safe-symbol-value check-symbol untag cell+ @ ;
: safe-symbol-function check-symbol untag 2 cells + @ ;

: #symbol-value ( symbol -- value )
    safe-symbol-value dup ::unbound = if void-variable endif ;

: #symbol-function ( symbol -- value )
    safe-symbol-function dup ::unbound = if void-function endif ;

\ Don't forget that #SET and #FSET words return the newly assigned
\ value, so if you are not going to use that value, drop it.
: #set ( symb value -- value )
    tuck swap check-symbol untag cell+ ! ;

: #fset ( symbol definition -- definition )
    tuck swap check-symbol untag 2 cells + ! ;

:noname
    #dosymbols
        dup
        dup safe-symbol-value copy #set drop
        dup safe-symbol-function copy #fset drop
    #endsymbols
; is copy-root-symbols    


\ Lisp basic conditional. It runs the true-branch if the top of the
\ stack is non-nil. It is compatible with `else' and `then' words.
: nil/= nil = not ;
: #if    `` nil/= `` if    ; imm-c/o
: #while `` nil/= `` while ; imm-c/o
: #until `` nil/= `` until ; imm-c/o


\ CONSes

variable allocated-conses

:noname
    untag 2 cells >tosp cons-tag tagged
; cons-tag copy-method!

: #cons ( x y -- cons )
    2 cells alloc-obj throw
    tuck cell+ ! tuck ! cons-tag tagged
    allocated-conses 1+! ;

: #consp tag cons-tag = >bool ;

: check-cons
    dup #consp nil = if wrong-type-argument endif ;

: #car dup #if check-cons untag @ endif ;
: #cdr dup #if check-cons untag cell + @ endif ;

\ Return the cdr of a cons. If the result is NIL, signals an error.
: assert-cdr
    #cdr dup nil = if parse-error endif ;

: #dolist
    `` begin
        `` dup `` #while
        `` dup `` >r
        `` #car
; imm-c/o
: #repeat
        `` r>
        `` #cdr
    `` repeat
    `` drop
; imm-c/o


\ SUBRS (primitive functions)

: special-subr? @ 0= ;
: subr>xt cell+ ;

  -1 constant infinite

: check-number-of-arguments ( n min max )
    >r over r> between if else wrong-type-argument endif ;

: non-eval-args ( list -- )
    0 swap #dolist swap 1+ #repeat ;

: eval-funcall-args ( list -- )
    0 swap #dolist eval-lisp-obj swap 1+ #repeat ;

\ Create a subr object (a primitive function to the Lisp system),
\ which accepts between MIN and MAX arguments, checks that the number
\ of arguments is correct and then call to the execution token XT.
: create-subr ( min max evaluated xt -- subr )
    2align here >r
    swap ,
    -rot swap 2dup `` literal `` literal `` check-number-of-arguments
    ( min max ) = if `` drop endif
    `` literal `` execute
    return
    r> subr-tag tagged ;

: trampoline >r dup true r> create-subr ;
: variadic-trampoline >r infinite true r> create-subr ;

: register-func ( n xt parse:name -- )
    parse-cname intern-symbol -rot trampoline #fset drop ;
: register-variadic-func ( n xt parse:name -- )
    parse-cname intern-symbol -rot variadic-trampoline #fset drop ;

2 ' #cons            register-func cons
1 ' #consp           register-func consp
1 ' #car             register-func car
1 ' #cdr             register-func cdr

1 ' #symbolp         register-func symbolp
1 ' #symbol-value    register-func symbol-value
1 ' #symbol-function register-func symbol-function
2 ' #set             register-func set
2 ' #fset            register-func fset

: FUNC ( n parse:name -- )
    latestxt register-func ;

: VARIADIC-FUNC ( n parse:name -- )
    latestxt register-variadic-func ;

: #subrp tag subr-tag = >bool ;
1 FUNC subrp

: execute-subr ( arg1 arg2 .. argn n subr -- ... )
    untag subr>xt execute ;


\ Integers

: >fixnum 2* 2* ;
: fixnum> 2/ 2/ ;

: #fixnump 3 and 0= >bool ; 1 FUNC fixnump
' #fixnump alias #integerp  1 FUNC integerp

: check-integer ( x -- x )
    dup #integerp nil = if wrong-type-argument endif ;

: 2-check-integers
    check-integer swap check-integer swap ;

: #= 2-check-integers = >bool ; 2 FUNC =
: #< 2-check-integers < >bool ; 2 FUNC <
: #> 2-check-integers > >bool ; 2 FUNC >
: #<= 2-check-integers <= >bool ; 2 FUNC <=
: #>= 2-check-integers >= >bool ; 2 FUNC >=
: #/= 2-check-integers = not >bool ; 2 FUNC >=

: #+ 2-check-integers + ; 2 FUNC +
: #- 2-check-integers - ; 2 FUNC -
: #* 2-check-integers fixnum> * ; 2 FUNC *
: #/ 2-check-integers / >fixnum ; 2 FUNC / 



: #list ( x1 x2 ... xn n -- list )
    nil swap 0 ?do #cons loop 
; 0 VARIADIC-FUNC list

: #length ( list -- n )
    0 swap #dolist drop 1+ #repeat >fixnum ;
1 FUNC length
    

\ Misc

: #eq = >bool ; 2 FUNC eq

: #not #if nil else t endif ; 1 FUNC not
' #not alias #null 1 FUNC null

: #quit quit-condition ;
0 FUNC quit


\ Reader

: digit-char? ( ch -- bool )
    [char] 0 swap [char] 9 between ;

: digit-value ( ch -- d )
    [char] 0 - ;

: whitespace-char? ( ch -- bool )
    case
        32 of true endof
        10 of true endof
        08 of true endof
        false swap
    endcase ;

: close-parent? [char] ) = ;

: discard-char
    parse-char drop ;

: skip-whitespaces
    begin peek-char whitespace-char? while discard-char repeat ;

: peek-conforming-char
    skip-whitespaces peek-char ;
    
: assert-char
    parse-char = invert if parse-error endif ;

: read-' discard-char [''] quote     read-lisp-obj 2 #list ;
: read-` discard-char [''] backquote read-lisp-obj 2 #list ;
: read-, discard-char [''] comma     read-lisp-obj 2 #list ;

: read-(... recursive
    peek-conforming-char case
        [char] ) of discard-char nil endof
        [char] . of
            discard-char read-lisp-obj
            skip-whitespaces [char] ) assert-char
        endof
        read-lisp-obj read-(... #cons swap
    endcase ;

: read-(
    discard-char peek-conforming-char [char] ) = if discard-char nil else
        read-lisp-obj read-(... #cons
    endif ;

: discard-line
    begin parse-char 10 = until ;

: read-;
    begin peek-conforming-char [char] ; = while discard-line repeat
    read-lisp-obj ;


32 constant token-buffer-size
create token-buffer token-buffer-size allot

: token-terminal-char? ( ch -- bool )
    dup whitespace-char? swap close-parent? or ;

: token-size
    token-buffer c@ ;

: full-token-buffer? ( -- bool )
    token-size token-buffer-size >= ;

: push-token-char ( ch -- )
    full-token-buffer? if drop else
        token-buffer 1+ token-size + c!
        token-size 1+ token-buffer c!
    endif ;

: read-token
    0 token-buffer c!
    begin parse-char push-token-char
    peek-char token-terminal-char? until
    token-buffer dup c@ 0= if parse-error endif ;

: try-unumber ( addr u -- d f )
    dup 0= if 2drop 0 0 exit then
    0 -rot
    0 ?do ( d addr )
        dup I + c@ digit-char? if
            swap 10 * over I + c@ digit-value + swap
        else
            unloop drop false exit
        endif
    loop
    drop true ;

: trim0 ( addr u -- addr+1 u-1 )
    dup if 1- swap 1+ swap endif ;

: try-number ( addr u -- d f )
    over c@ case
        [char] - of trim0 try-unumber swap negate swap endof
        [char] + of trim0 try-unumber endof
        drop try-unumber 0
    endcase ;

: >sym/num ( c-addr -- x )
    dup count try-number if
        nip >fixnum
    else
        drop intern-symbol
    endif ;

: #read ( -- x )
    peek-conforming-char case
        [char] ( of read-( endof
        [char] ' of read-' endof
        [char] ` of read-` endof
        [char] , of read-, endof
        [char] ; of read-; endof
        [char] . of parse-error endof
               0 of eof-condition endof
        drop read-token >sym/num 0
    endcase ;

' #read is read-lisp-obj
0 FUNC read


\ Printer

defer print-lisp-obj

: print-integer fixnum> print-number ;
: print-symbol symbol-name count type ;

: print-list
    [char] ( emit
    dup #car print-lisp-obj #cdr
    begin dup #consp #while
        space dup #car print-lisp-obj #cdr
    repeat
    \ Trailing element
    dup #if
        ."  . " print-lisp-obj ." )"
    else
        drop [char] ) emit
    endif ;

: #print ( x -- )
    dup #symbolp  #if print-symbol  exit endif
    dup #integerp #if print-integer exit endif
    dup #consp    #if print-list    exit endif
\   Unreadable objects
    dup #subrp    #if drop ." #<subr object>" exit endif
    drop wrong-type-argument ;
' #print is print-lisp-obj
1 FUNC print


\ Interpreter

: eval-if
    assert-cdr
    dup #car eval-lisp-obj #if
        assert-cdr #car eval-lisp-obj
    else
        assert-cdr #cdr #car eval-lisp-obj
    endif ;

: eval-progn-list ( list -- x )
    nil swap
    begin
        nip dup #car eval-lisp-obj swap
    #cdr dup #null #until
    drop ;

: eval-progn
    #cdr eval-progn-list ;

\ Funcalls

: lambda-args assert-cdr #car ;
: lambda-nargs lambda-args #length fixnum> ;
: lambda-body assert-cdr #cdr ;

\ Swap the values of the cell pointed by ADDR and the value of SYMBOL.
: cell<->symbol ( addr symbol -- )
    dup safe-symbol-value -rot over @ #set drop ! ;

\ Iterate on the arguments of the lambda, swapping the argument in the
\ stack by the value slot of the symbol.
: stack<->symbols ( a1 a2 ... an n symbs -- v1 v2 ... vn n symbs )
    2dup 2>r swap 1+ cells sp + swap
    #dolist 2dup cell<->symbol drop cell - #repeat
    drop 2r> ;

: funcall-lambda ( arg1 arg2 ... argn n lambda -- x )
    2dup lambda-nargs = not if wrong-number-of-arguments then
    dup >r lambda-args stack<->symbols
    r> lambda-body eval-progn-list >r
    stack<->symbols drop ndrop r> ;

: funcall ( arg1 ... argn n function -- x)
    >r pinargs r> over >r
    dup #symbolp #if #symbol-function endif
    dup #subrp #if execute-subr else funcall-lambda endif
    r> unpinargs ;

: eval-funcall ( list -- x )
    dup >r #cdr eval-funcall-args r> #car funcall ;

: #funcall ( function arg1 arg2 ... argn n+1 --- x )
    1- dup >r roll r> swap funcall ;
1 VARIADIC-FUNC funcall

\ is X a symbol which designates a macro?
: macro? ( x -- bool)
    dup #symbolp #not #if drop false exit endif
    safe-symbol-function
    dup #consp #not #if drop false exit endif
    #car [''] macro = ;

: macroexpand-1*
    dup >r #cdr non-eval-args r>
    #car #symbol-function #cdr funcall ;

: #macroexpand-1 ( list -- x )
    dup #consp #not #if exit endif
    dup #car macro? if
        macroexpand-1*
    endif ;
1 FUNC macroexpand-1

\ Non-atoms

: eval-list
    dup #car case
        [''] quote of #cdr #car endof
        ['']    if of eval-if endof
        [''] progn of eval-progn endof
        macro? if
            #macroexpand-1 eval-lisp-obj
        else
            eval-funcall
        endif
        0 \ any element here
    endcase ;

: #eval ( x -- y )
    dup #integerp #if exit endif
    dup #symbolp  #if #symbol-value exit endif
    dup #consp    #if eval-list exit endif
    wrong-type-argument
; ' #eval is eval-lisp-obj
1 FUNC eval


\ REPL

defer repl-function

: repl-iteration #read #eval ;
: user-repl-iteration ." * " query #read #eval #print CR ;

: process-toplevels
    begin repl-function again ;

\ Process Lisp forms until an error is signaled.
: repl-loop ( repl-iteration-word -- )
    ['] process-toplevels catch case
            0 of endof
            1 of ." ERROR: void variable" CR endof
            2 of ." ERROR: void function" CR endof
            3 of ." ERROR: wrong type of argument" CR endof
            4 of ." ERROR: wrong number of arguments" CR endof
            5 of ." ERROR: parsing error" CR endof
            \ 6 of endof EXIT
            \ 7 of endof EOF
            throw
        endcase ;

\ Process forms until EXIT or EOF conditions.
: toplevel
    ['] user-repl-iteration is repl-function
    begin
        ['] repl-loop catch case
            6 of exit endof
            7 of exit endof
        endcase
    again ;

.( Loading core.lisp...) CR
@lisp/core.lisp buffer>string
:noname
    ['] repl-iteration is repl-function
    ['] repl-loop catch case
        6 of endof
        7 of endof
    endcase
; execute-parsing

: run-lisp
    page 0 0 at-xy ." RUNNING EULEX LISP." CR CR
    refill-silent? on
    get-order get-current
    in-lisp-package: definitions
    end-newline-p on
    toplevel
    end-newline-p off
    set-current set-order
    refill-silent? off
    CR ." GOOD BYE!" CR CR ;

\ Provide RUN-LISP in the system vocabulary
set-current
' run-lisp alias run-lisp
previous previous

\ lisp.fs ends here
