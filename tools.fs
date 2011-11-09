\ tools.fs --

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

: addr-column
    print-hex-number [char] : emit space ;

\ Dump n bytes of the memory from ADDR, in a readable way.
: dump ( addr n -- )
    0 ?do
        i 16 mod 0= if
            cr dup i + addr-column
        then
        dup i + c@ print-hex-byte space
    loop drop
    cr
;

: map-nt ( xt -- )
    >r
    context @ @
    begin
    dup 0<> while
        dup r> dup >r execute
        previous-word
    repeat
    r>
    2drop ;
:noname nt>name type space ;
: words literal map-nt ;

variable count_words

:noname drop count_words 1+! ;
: stats
    count_words 0!
    literal map-nt
    ." Words in the context: " count_words @ . cr
    ." Dictionary space allocated: " dp dp-base - . ." bytes" cr
;


\ Display the content of the variable ADDR.
: ? ( addr -- )
    @ . ;


\ Display the data stack
: .s
    ." <" depth print-number ." > "
    sp-limit
    begin
        cell -
    dup sp cell + > while
        dup @ .
    repeat
    drop ;


\ Backtrace!

: retaddr>xt ( x -- )
    dup cell - @ + ;

: backtrace-frame ( addr -- )
    retaddr>xt unfind dup 0<> if
        2 spaces type cr
    else
        type
    endif ;

\ Display the current backtrace.
: backtrace
    ." Backtrace: " cr
    rsp
    begin
    dup rsp-limit <= while
        dup @ backtrace-frame
        cell +
    repeat
    drop
;


require @disassem.fs


\ tools.fs ends here
