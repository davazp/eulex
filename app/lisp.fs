\ lisp.fs --- A straighforward dynamically-scoped Lisp

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

\ TODO: Implement a basic GC!

vocabulary lisp
get-current
also eulex
also lisp definitions

3 constant tag-bits
1 tag-bits lshift 1 - constant tag-mask

%000 constant even-fixnum-tag
%100 constant odd-fixnum-tag
%001 constant cons-tag
%011 constant symbol-tag
%101 constant subr-tag

: tagged or ;
: ?tagged swap dup 0= if nip else swap tagged then ;
: untag tag-mask invert and ;

\ Errors
: void-variable 1 throw ;
: void-function 2 throw ;
: wrong-type-argument 3 throw ;
: wrong-number-of-arguments 4 throw ;

\ Symbols

\ We write the lisp package system upon wordlists. The PF of the words
\ contains the symbol value and the symbol function parameters aligned
\ to a double cell size.
wordlist constant lisp-package
lisp-package >order

: in-lisp-package:
    lisp-package 1 set-order ;

: create-in-lisp-package
    get-order get-current in-lisp-package: definitions
    create set-current set-order ;

: find-cname-in-lisp-package ( c-addr -- nt|0 )
    >r get-order in-lisp-package: r>
    find-cname >r set-order r> ;

: create-symbol
    create-in-lisp-package 2align does> 2aligned symbol-tag tagged ;

: ::unbound [ here 2aligned symbol-tag tagged ]L ;

create-symbol t     t , ::unbound ,
create-symbol nil nil , ::unbound ,

create-symbol quote ::unbound , ::unbound ,

: >bool if t else nil then ;
: bool> nil = if 0 else -1 then ;

: find-symbol ( c-addr -- symbol|0 )
    find-cname-in-lisp-package dup if nt>xt execute endif ;

: intern-symbol ( c-addr -- symbol )
    dup find-symbol ?dup if nip else
        count nextname create-symbol ::unbound , ::unbound ,
        latestxt execute 
    then ;

: #symbolp
    tag-mask and symbol-tag = >bool ;

\ Check if X is a symbol object. If not, it signals an error.
: check-symbol ( x -- x )
    dup #symbolp NIL = if wrong-type-argument then ;

: #symbol-value ( symbol -- value )
    check-symbol untag @
    dup ::unbound = if void-variable endif ;

: #symbol-function ( symbol -- value )
    check-symbol untag cell + @
    dup ::unbound = if void-function endif ;

: #set ( symb value -- )
    swap check-symbol untag ! ;

: #fset ( symbol definition -- )
    swap check-symbol untag cell + ! ;
    
\ Lisp basic conditional. It runs the true-branch if the top of the
\ stack is non-nil. It is compatible with `else' and `then' words.
: #if
    [ nil ]L postpone literal
    postpone =
    postpone not
    postpone if
; immediate compile-only

\ Subrs (primitive functions)

: check-number-of-arguments
    = not if wrong-number-of-arguments endif ;

\ Create a subr object (a primitive function to the Lisp system),
\ which accepts N arguments, checks that the number of arguments is
\ correct and then call to the execution token XT.
: trampoline ( n xt -- subr )
    2align here >r
    swap postpone literal
    postpone check-number-of-arguments
    postpone literal
    postpone execute
    return
    r> subr-tag tagged ;

\ Parse a word and intern a symbol for it, with a function value which
\ accepts N arguments and calls to XT.
: register-func ( n xt parse:name -- )
    parse-cname intern-symbol -rot trampoline #fset ;

1 ' #symbolp         register-func symbolp
1 ' #symbol-function register-func symbol-function
1 ' #symbol-value    register-func symbol-value
2 ' #set             register-func set
2 ' #fset            register-func fset

: FUNC ( n parse:name -- )
    latestxt register-func ;

: #subrp
    tag-mask and subr-tag = >bool ;
1 FUNC subrp

: funcall-subr ( arg1 arg2 .. argn n subr -- ... )
    untag execute ;


\ Integers

: >fixnum [ tag-bits 1 - ]L lshift ;
: fixnum> [ tag-bits 1 - ]L rshift ;

: #fixnump 1 and 0= >bool ; 1 FUNC fixnump
' #fixnump alias #integerp  1 FUNC integerp

: check-integer ( x -- x )
    dup #integerp NIL = if wrong-type-argument endif ;

: 2-check-integers
    check-integer swap check-integer swap ;

: #= = >bool ; \ 2 FUNC =
: #< < >bool ; \ 2 FUNC <
: #> > >bool ; \ 2 FUNC >
: #<= <= >bool ; \ 2 FUNC <=
: #>= >= >bool ; \ 2 FUNC >=
: #/= = not >bool ; \ 2 FUNC >=

: #+ + ; \ 2 FUNC +
: #- - ; \ 2 FUNC -
: #* fixnum> * ; \ 2 FUNC *
: #/ / >fixnum ; \ 2 FUNC / 

\ CONSes

variable allocated-conses

: #cons ( x y -- cons )
    2 cells allocate throw
    tuck cell+ ! tuck ! cons-tag tagged
    allocated-conses 1+! ;
2 FUNC cons

: #consp tag-mask and cons-tag = >bool ;
1 FUNC consp

: check-cons
    dup #consp nil = if wrong-type-argument endif ;

: #car dup #if check-cons untag @ endif ;
1 FUNC car

: #cdr dup #if check-cons untag cell + @ endif ;
1 FUNC cdr

: list ( x1 x2 ... xn n -- list )
    nil swap 0 ?do #cons loop ;

\ Misc

: #eq = >bool ; 2 FUNC eq


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

defer read-lisp-obj

: discard-char
    parse-char drop ;

: skip-whitespaces
    begin peek-char whitespace-char? while discard-char repeat ;

: read-'
    discard-char quote read-lisp-obj 2 list ;

: read-(... recursive
    skip-whitespaces
    peek-char case
        [char] ) of discard-char nil endof
        [char] . of discard-char read-lisp-obj endof
        read-lisp-obj read-(... #cons swap
    endcase ;
: read-( discard-char read-(... ;


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
    begin parse-char
    dup token-terminal-char? not while
        push-token-char
    repeat
    drop token-buffer ;

: try-unumber ( addr u -- d f )
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
    peek-char case
        [char] ( of read-( endof
        [char] ' of read-' endof
        drop read-token >sym/num 0
    endcase ;

' #read is read-lisp-obj
0 FUNC read


\ Interpreter

: run-lisp
    page 0 0 at-xy ." RUNNING EULEX LISP." cr ;

previous previous set-current

\ Provide RUN-LISP in the system vocabulary
latestxt alias run-lisp

\ Local Variables:
\ forth-local-words: ((("#if") compile-only (font-lock-keyword-face . 2)))
\ End:

\ lisp.fs ends here
