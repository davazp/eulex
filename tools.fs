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
    attr gray swap print-hex-number [char] : emit space attr! ;

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
    context @ dowords
        dup r@ execute
    endwords
    r> drop ;

: words ['] id. map-nt ;

: room-count ( -- n )
    0 context @ dowords swap 1+ swap endwords ;
: room
    CR
    ." Words in the context: " room-count . CR
    ." Dictionary space allocated: " dp dp-base - . ." bytes" cr ;


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
    dowords
        2dup nt>xt = if
            nip nt>name
            exit
        then
    endwords
    drop
    0 0 ;

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

: backtrace-frame ( addr -- flag )
    retaddr>xt unfind dup 0<> if
        2 spaces type cr true
    else
        type false
    endif ;

\ Display the current backtrace.
variable backtrace-limit
10 backtrace-limit !
: backtrace
    ." Backtrace: " cr
    backtrace-limit @
    rsp
    begin over 0<> over rsp-limit <= and while
        dup @ backtrace-frame if swap 1- swap endif
        cell +
    repeat
    drop ;


( Display the list of vocabularies in the system and the search order stack )

: .wid ( wid -- )
    wid>name ?dup if type space else ." ??? " drop then ;

: vocs
    last-wid @
    begin ?dup while
        dup .wid
        wid-previous @
    repeat ;

: order
    get-order 0 ?do .wid loop 4 spaces current @ .wid ;

Root definitions
' order alias order
' words alias words
previous definitions

( Disassembler. SEE )
require @disassem.fs


\ Dynamic memory management debugging

: .chunk ( chunk -- )
    ." Base: " dup chunk>addr print-hex-number 5 SPACES
    ." End:  " dup chunk>end  print-hex-number 5 SPACES
    ." Size: "     chunk>size print-hex-number CR ;

: meminfo
    CR first-chunk
    begin dup null-chunk? not while
        dup .chunk
        next-chunk
    repeat
    drop ;


\ tools.fs ends here
