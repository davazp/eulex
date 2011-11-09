\ output.fs --

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


: cr 10 emit ;
: space 32 emit ;
: spaces
    dup 0 > if
        0 ?do space loop
    else
        drop
    then ;

create .buffer 32 allot
variable .index

: sign
    0< if
        [char] - emit
    then ;

: a@ ( addr n -- x )
    + c@ ;

: a! ( x addr n -- )
    + c! ;

: emit-hex-digit
    dup 10 < if
        [char] 0 +
    else
        [char] A 10 - +
    then
    emit ;

: .dump_buffer
    .index @ 0 ?do
        -1 .index +!
        .buffer .index @ a@ emit-hex-digit
    loop ;

: print-number
    ?dup 0= if
        [char] 0 emit
        exit
    then
    dup sign abs
    .index 0!
    begin
        dup base @ mod .buffer .index @ a!
        1 .index +!
        base @ /
    dup 0= until drop
    .dump_buffer ;


: print-basis ( basis n -- )
    base @ >r
    swap base ! print-number
    r> base ! ;

: . print-number space ;

: ? ( addr -- ) @ . ;

: hex. 16 swap print-basis space ;
: dec. 10 swap print-basis space ;
: oct.  8 swap print-basis space ;

: print-hex-byte ( n -- )
    dup 16 / emit-hex-digit
    16 mod emit-hex-digit
;

: print-hex-number ( n -- )
    [char] 0 emit
    [char] x emit
    dup 28 rshift 15 and emit-hex-digit
    dup 24 rshift 15 and emit-hex-digit
    dup 20 rshift 15 and emit-hex-digit
    dup 16 rshift 15 and emit-hex-digit
    dup 12 rshift 15 and emit-hex-digit
    dup  8 rshift 15 and emit-hex-digit
    dup  4 rshift 15 and emit-hex-digit
         0 rshift 15 and emit-hex-digit ;


\ Strings

: type ( addr n )
    0 ?do
        dup c@ emit
        1+
    loop
    drop
;


: ."
    [compile] s"
    if-compiling
        postpone type
    else
        type
    then
; immediate


\ output.fs ends here
