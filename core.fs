( core.fs --- Basic definitions )

\ Copyright 2011 (C) David Vazquez

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

: noop ;
: chars ;
: char+ 1 chars + ;
: cell 4 ;
: cells cell * ;
: cell+ 1 cells + ;
: here dp ;
: allot dp + dp! ;
: , here cell allot ! ;
: c, here 1 allot c! ;
: variable create 0 , ;
: constant create , does> @ ;
: false 0 ;
: true -1 ;
: on true swap ! ;
: off false swap ! ;

: 0< 0 < ;
: 0= 0 = ;
: 0> 0 > ;
: 1+ 1 + ;
: 1- 1 - ;
: 2+ 2 + ;
: 2- 2 - ;
: 2* 2 * ;
: negate 0 swap - ;
: not 0= ;
: <= > not ;
: >= < not ;
: <> = not ;
: 0<> 0 <> ;
: 0! 0 swap ! ;
: +! dup @ rot + swap ! ;
: 1+! 1  swap +! ;
: 1-! -1 swap +! ;
: and! dup @ rot and swap ! ;
: or!  dup @ rot or swap ! ;

: bit? ( x n -- flag )
    1 swap lshift and 0<> ;
: CF? ( -- flag )
    eflags  0 bit? ;
: SF? ( -- flag)
    eflags 7 bit? ;
: OF? ( -- flag )
    eflags 11 bit? ;

: -rot rot rot ;
: tuck swap over ;
: 2dup over over ;
: 2drop drop drop ;
: 2swap >r -rot r> -rot ;
: 2nip 2>r 2drop 2r> ;

: /mod 2dup / >r mod r> ;

: clearstack sp-limit sp! ;
: depth sp-limit sp - cell / 1- ;

create pad 1024 allot

\ Dictionary's entries (name token -- NT )

\ Get the NT of the last-defined word.
: nt>name ( nt -- addr u )
    dup c@ swap 1+ swap ;
: previous-word
    cell - @ ;

: nt>flags ( nt -- flags )
    dup c@ + 1+ ;
: nt>cfa
    nt>flags 1+ ;
: nt>pfa
    nt>cfa cell + ;
: nt>xt
    nt>cfa @ ;

: latest
    latest_word @ ;
: latestxt
    latest nt>xt ;
: immediate? ( word -- flag )
    nt>flags c@ 1 and ;

: immediate
    latest nt>flags 1 swap or! ;
: compile-only
    latest nt>flags 2 swap or! ;

: [ 0 state ! ; immediate
: ] 1 state ! ;

: parse-nt
    parse-cname find-cname ;

: nt>comp
    dup immediate? swap nt>xt ;

: comp'
    parse-nt nt>comp ;

: '
    comp' nip ;

' noop alias )

( Skip page breaks. They can be beautiful as section delimiters )
:

; immediate

: compile,
    \ call ADDR
    $e8 c, here cell + - , ;

\ Metametacompiler
: postpone-non-immediate, ( xt -- )
    \ Compile into dictionary the following code:
    $b8 c, ( ' ) ,            \ movl $[ADDR], %eax
    $c7 c, $07 c, $e8 ,       \ movl $0xe8, (%edi)
    $47 c,                    \ incl %edi
    $29 c, $f8 c,             \ subl %edi, %eax
    $83 c, $e8 c, $04 c,      \ subl $4, %eax
    $89 c, $07 c,             \ movl %eax, (%edi)
    $83 c, $c7 c, $04 c,      \ addl $4, %edi
    \ which, when is executed, compiles a CALL ADDR.
;

\ Partial implementation of POSTPONE, it works for non-immediate words.
\ [COMPILE] words for immedaite words, but we cannot use IF, therefore
\ we cannot define POSTPONE properly yet.
: postpone ' postpone-non-immediate, ; immediate

: push
    $83 c, $EE c, $04 c,              \ subl $4, %esi
    $c7 c, $06 c,  ( ... )            \ mov $..., (%esi)
; compile-only

: literal push , ; immediate compile-only

: forward-literal ( -- addr )
    push here 0 , ;

: patch-forward-literal ( addr n -- )
    swap ! ;

: branch
    $e9 c,               \ jmp
; compile-only

: ?branch
    $8b c, $06 c,         \ movl (%esi), %eax
    $83 c, $c6 c, $04 c,  \ addl $4, %esi
    $85 c, $c0 c,         \ test %eax, %eax
    $0f c, $85 c,         \ jnz [ELSE]
; compile-only

: branch-to ( addr -- )
    branch here cell + - , ;

: ?branch-to ( addr -- )
    ?branch here cell + - , ;

: forward-branch   branch here 0 , ;

: forward-?branch ?branch here 0 , ;

: patch-forward-branch ( jmp-addr target -- )
    over cell + - swap ! ;

\ BEGIN-UNTIL
\ BEGIN-WHILE-REPEAT
\ BEGIN-AGAIN

: begin ( -- begin-addr )
    here
; immediate compile-only

: until ( begin-addr -- )
    postpone not
    ?branch-to
; immediate compile-only

: while ( begin-addr -- begin-addr while-addr )
    postpone not
    forward-?branch
; immediate compile-only

: repeat ( begin-addr while-addr -- )
    swap
    branch-to
    here patch-forward-branch
; immediate compile-only

: again ( begin-addr -- )
    branch-to
; immediate compile-only


\ IF-THEN
\ IF-ELSE-THEN

: if ( -- if-forward-jmp )
    postpone not
    forward-?branch
; immediate compile-only

: else ( if-forward-jmp -- else-for)
    forward-branch swap
    here patch-forward-branch
; immediate compile-only

: then
    here patch-forward-branch
; immediate compile-only
' then alias endif immediate compile-only

: ?dup
    dup 0<> if dup then ;

: postpone,
    swap if compile, else postpone-non-immediate, endif ;

: postpone
    comp' postpone, ; immediate

: ]L ] postpone literal ;

\ DO-UNTIL
\ DO-WHILE-REPEAT

: do ( -- null-forward-branch do-addr )
    postpone 2>r
    \ null forward-branch
    0
    here
; immediate compile-only

: ?do ( -- forward-branch do-addr )
    postpone 2dup
    postpone 2>r
    postpone =
    forward-?branch
    here
; immediate compile-only

\ Check if LIMIT and INDEX are such that we
\ shouldn't go ahead with the ?DO..+LOOP iteration.
: +endloop? ( n limit index -- flag )
    swap
    - sf? >r
    swap - sf? nip r>
    <> ;

: +loop ( COMPILEATION: forward-branch do-addr --
          RUNTIME: n -- )
    \ Update the index
    postpone dup
    postpone r>
    postpone +
    postpone >r
    \ Check conditions
    postpone 2r@
    postpone +endloop?
    postpone not
    ?branch-to
    ?dup 0<> if
        here patch-forward-branch
    then
    postpone 2r>
    postpone 2drop
; immediate compile-only

: loop ( forward-branch do-addr -- )
    1
    postpone literal
    postpone +loop
; immediate compile-only

: unloop
    \ We are careful in order not to corrupt the caller's pointer.
    rsp @
    rsp 2 cells + rsp!
    rsp !
; compile-only

: leave
    postpone 2r>
    postpone nip
    postpone dup
    postpone 1-
    postpone 2>r
; immediate compile-only


: i rsp 1 cells + @ ; compile-only
: j rsp 3 cells + @ ; compile-only
: k rsp 5 cells + @ ; compile-only

: abs
    dup 0< if negate then ;

: max
    2dup < if nip else drop then ;

: min
    2dup > if nip else drop then ;

: fill ( c-addr u c -- )
    -rot 0 ?do 2dup c! 1+ loop 2drop ;

: pick  ( xn ... x0 u -- xn ... x0 xu )
    1+ cells sp + @ ;

: roll ( xn ... x1 x0 n  --  xn-1 .. x0 xn )
    dup 1+ pick >r
    0 swap ?do \ Replace x_i por x_{i-1}
        sp i 1- cells + @
        sp i 1+ cells + ! \ Note that tehre is an extra element due to the previous line.
    -1 +loop
    drop r>
;

: 2over 3 pick 3 pick ;
: 2tuck 2swap 2over ;
: 2rot 5 roll 5 roll ;

\ Like ALLOT but initialize memory to 0.
: zallot ( n -- )
    dup 0 < if
        allot
    else
        here swap
        dup allot
        0 fill
    endif ;


: low-byte 255 and ;
: high-byte 8 rshift low-byte ;

: printable-char? ( ch -- flag )
    dup  $20 >=
    swap $7e <= and ;

\ Facility for defining harmful state-smartess words.
\    I did not know, when I wrote this, what state-smartness were bad.
\    So, if you want to learn well Forth, you should not read this code,
\    probably.
: if-compiling
    postpone state
    postpone @
    postpone if
; immediate

: [']
    ' postpone literal
; immediate compile-only


: [char] char postpone literal ; immediate

\ CASE's implementation imported from Gforth.
\
\ Usage
\ ( n )
\ CASE
\    1 OF .... ENDOF
\    2 OF .... ENDOF
\    OTHERWISE
\ END-CASE
\
\ Remember not to consume the element in the OTHERWISE case.

0 constant case immediate

: of
    1+ >r
    postpone over
    postpone =
    postpone if
    postpone drop
    r>
; immediate

: endof
    >r postpone else r>
; immediate

: endcase
    postpone drop
    0 ?do postpone then loop
; immediate


\ Interprete a string

: buffer>start ( addr -- start )
    @ ;

: buffer>size ( addr -- size )
    cell + @ ;

: buffer>loaded ( addr -- load-var )
    2 cells + ;

: buffer-loaded? ( addr -- flag )
    buffer>loaded @ ;

: mark-buffer-as-loaded ( addr -- )
    buffer>loaded true swap ! ;
@core.fs mark-buffer-as-loaded

: load-buffer ( addr -- )
    dup mark-buffer-as-loaded
    dup buffer>start
    swap buffer>size
    evaluate ;

: require-buffer ( addr -- )
    dup buffer-loaded? if drop else load-buffer then ;


\ Recursion

: recurse latestxt compile, ; immediate compile-only
: recursive latestxt current @ ! ; immediate

\ Enumerations. See kernel/irq.fs for usage.
: enum dup constant 1+ ;
: end-enum drop ;

@structures.fs require-buffer
@exceptions.fs require-buffer

\ Complete the following definitions to support error handling.
: nt'
    parse-nt dup 0= if -13 throw then ;
: comp'
    nt' nt>comp ;
: '
    comp' nip ;
: [nt']
    nt' postpone literal ; immediate compile-only
: [comp']
    comp' postpone literal ; immediate compile-only
: [']
    ' postpone literal ; immediate compile-only
: postpone
    comp' postpone, ; immediate
: [compile]
    ' compile, ; immediate

\ Parse a word from input buffer and store N in its PFA.
: 'pfa! ( n -- )
    nt' nt>pfa
    if-compiling
       postpone literal
       postpone !
    else
        !
    endif
; immediate


\ Values

: VALUE ( n -- )
    create , does> @ ;

: TO ( n -- )
    postpone 'pfa! ; immediate


\ Defered words
: DEFER
    create ['] abort , does> @ execute ;
: IS
    postpone 'pfa! ; immediate


\ A syntax sugar for require-buffer
: require
    '
    if-compiling
        postpone literal
        postpone execute
        postpone require-buffer
    else
        execute
        require-buffer
    endif
; immediate

: include
    '
    if-compiling
        postpone literal
        postpone execute
        postpone load-buffer
    else
        execute
        require-buffer
    endif
; immediate


require @interpreter.fs
require @math.fs
require @string.fs

: count ( c-addr -- addr u )
    dup c@ swap 1+ swap ;

: c>addr ( addr u addr -- )
    2dup 2>r 1+ swap move 2r> c! ;

: c-addr ( addr u -- c-addr )
    pad c>addr pad ;

: parse-name ( -- addr u )
    parse-cname count ;

\ NAMED AND NONAMED WORDS

create nextname-buffer 32 allot

: nextname ( addr u -- )
    nextname-buffer c>addr
    nextname-buffer compiling_nextname ! ;

: noname 0 0 nextname ;
: :noname noname : latestxt ;

\ If flag is true, parse a name of word and create a alias for NOOP,
\ otherwise the parsed word is discarded. It is used to define
\ features in the environment as #define in C. You can use
\ [ifdef]..[endif] to check the availability of features.
: feature ( flag -- )
    parse-name rot if
        nextname ['] noop alias
    else
        2drop
    endif ;

require @vocabulary.fs
require @kernel/console.fs
require @output.fs
page ." Loading..." cr
require @kernel/multiboot.fs
require @tools.fs
require @kernel/interrupts.fs
require @kernel/exceptions.fs
require @kernel/irq.fs
require @kernel/timer.fs
require @kernel/keyboard.fs
require @kernel/serial.fs
require @kernel/speaker.fs
require @tests/tests.fs

require @kernel/cpuid.fs


\ Rebooting the machine

: reboot
    beep
    disable-interrupts
    clear-kbd-buffer
    kdb-reset kbd-io outputb
    halt ;

: 3dup 2 pick 2 pick 2 pick ;


( run-tests )

\ DEBUGGING. This is useful to run the QEMU on emacs, and use Eulex
\ like anyother Forth implementation!

\ : serial-loop
\     ." Initializing serial port interface..." cr
\     ['] read-byte input_routine ! ;

\ serial-echo-on
\ serial-loop

:noname  -3 throw ; stack_overflow_err_routine !
:noname  -4 throw ; stack_underflow_err_routine !
:noname -13 throw ; unknown_word_err_routine !
:noname -14 throw ; compile_only_err_routine !

enable-interrupts
require @user.fs
