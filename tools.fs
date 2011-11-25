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

: id. nt>name type space ;
: words ['] id. map-nt ;

variable count_words

:noname drop count_words 1+! ;
: stats
    cr
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


: unfind-in-wordlist ( xt wordlist -- addr c )
    wid>latest
    begin
        dup 0<> while
            2dup nt>xt = if
                nip nt>name
                exit
            else
                previous-word
            then
    repeat
    2drop
    0 0
;

\ Find the first avalaible word whose CFA is XT, pushing the name to
\ the stack or two zeros if it is not found.
: unfind ( xt -- addr u )
    get-order dup 1+ roll
    ( widn ... wid1 n xt )
    begin
        over 0<> while
            swap 1- swap rot
            over swap unfind-in-wordlist
            dup 0= if
                2drop
            else
                >r >r
                drop 0 ?do drop loop
                r> r>
                exit
            then
    repeat
    2drop
    0 0
;

\ Backtrace!

: upper@ ( addr -- x|0)
    dup mem-upper-size u< if @ else drop 0 endif ;

: retaddr>xt ( x -- )
    dup cell - upper@ + ;

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


( Display the list of vocabularies in the system )

: vocs-print-vocentry ( ve -- )
    vocentry>name type space ;

: vocs
    vocentry-root @
    begin
    ?dup while
        dup vocs-print-vocentry
        vocentry-previous @
    repeat ;


( Display the order stack and the current word list )

: wid>name ( wid -- addr n )
    vocentry-root @
    begin
    ?dup while
        2dup vocentry-wid @ = if
            nip vocentry>name exit
        else
            vocentry-previous @
        endif
    repeat
    drop 0 0 ;

: anonymous-wid? ( wid -- )
    wid>name nip 0= ;

: print-wid ( wid -- )
    dup anonymous-wid? if
        drop ." ??? "
    else
        wid>name type space
    endif ;

: order
    get-order 0 ?do print-wid loop
    4 spaces current @ print-wid ;

Root definitions
' order alias order
' words alias words
previous definitions

( Disassembler. SEE )
require @disassem.fs

\ tools.fs ends here
