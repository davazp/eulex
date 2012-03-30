\ user.fs --

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

page
." Welcome to Eulex!" cr
cr
." Copyright (C) 2011,2012 David Vazquez" cr
." This is free software; see the source for copying conditions.  There is NO"  cr
." warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE." cr cr

: license
    ." This program is free software; you can redistribute it and/or modify" cr
    ." it under the terms of the GNU General Public License as published by" cr
    ." the Free Software Foundation; either version 3 of the License, or" cr
    ." (at your option) any later version." cr
    cr
    ." This program is distributed in the hope that it will be useful," cr
    ." but WITHOUT ANY WARRANTY; without even the implied warranty of" cr
    ." MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the" cr
    ." GNU General Public License for more details." cr
    cr
    ." You should have received a copy of the GNU General Public License" cr
    ." along with this program. If not, see http://www.gnu.org/licenses/." cr ;


variable error-message
variable error-message-size

: exception-message ( -- addr u )
    error-message @
    error-message-size @ ;
: exception-message! ( addr u -- )
    error-message-size !
    error-message ! ;

: (abort)" exception-message! -2 throw ;

: abort"
    postpone if
    postpone s"
    postpone (abort)"
    postpone then
; immediate compile-only

: catch-errors ( xt -- )
    %catch-without-unwind ?dup 0<> if
        CR ." ERROR: "
        case
            -1 of ." Aborted" cr endof
            -2 of exception-message type CR endof
            -3 of ." Stack overflow" cr endof
            -4 of ." Stack underflow" cr endof
            -10 of ." Division by zero" cr endof
            -13 of ." Unknown word" cr endof
            -14 of ." Compile-only word" cr endof
            ." Ocurred an unexpected error of code " dup . cr
        endcase
        ." >>>" read_word_buffer count type ." <<<" cr
        backtrace
        state 0!
        clearstack
    then
    %unwind-after-catch ;

:noname @eulexrc.fs require-buffer ;
: load-eulexrc catch-errors ;

: user-interaction query interpret ;
: user-interaction-loop
    begin ['] user-interaction catch-errors again ;

: start-user-interaction
    only forth definitions also
    load-eulexrc
    user-interaction-loop ;


\ Export words to the Forth vocabulary

: clone-word ( nt -- )
    dup nt>name nextname
    dup nt>xt alias
    nt>flags @ latest nt>flags ! ;

: }
    set-current ;

: FORTH{
    get-current
    forth-wordlist set-current
    begin
    NT'
    dup [NT'] } <> while
        clone-word
    repeat
    nt>xt execute ;

FORTH{
! ' ( ) * + +! +loop , - -rot -trailing . ." .( .s / /mod /string 0!
0< 0<> 0= 0> 1+ 1- 2* 2+ 2- 2>r 2drop 2dup 2nip 2over 2r> 2r@ 2rot
2swap 2tuck : :noname ; < <= <> <count-spaces = > >= >in >order >r ?
?do ?dup @ Forth Only Root [ ['] [char] [compile] [defined] [else]
[endif] [if] [ifdef] [ifundef] [then] \ ] ]L abort abort" abs accept
again alias align aligned allocate allot also and at-xy base beep
begin blank c! c, c@ case catch cell cell+ cells char char+ chars
clearstack cmove cmove> compare compile, compile-only constant context
count cr create current dec. decimal defer definitions depth do does>
drop dump dup edit-line else emit end-struct endcase endif endof eulex
evaluate execute exit false field fill free gcd get-current get-order
here hex hex. i id. if immediate invert is j k key latest latestxt lcm
leave literal loop lshift max min mod move ms negate nextname nip
noname noop not oct. octal of off on or order over pad page parse-name
pick postpone previous query r> r@ reboot recurse recursive refill
repeat restore-input resize roll room rot rshift s" save-input see
set-current set-order sign source source-id space spaces state
string-prefix? string<> string= struct swap then throw tib to true
tuck type typewhite u< unloop until value variable vocabulary vocs w!
w@ while wordlist words xor
}


START-USER-INTERACTION

\ user.fs ends here
