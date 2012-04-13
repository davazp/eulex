\ Strings

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

create read-string-buffer 256 allot
variable read-string-index

: rs,
    read-string-buffer read-string-index @ + c!
    read-string-index 1+! ;

: read-string
    read-string-index 0!
    read-string-buffer
    0
    begin
        parse-char
    dup [char] " <> while
        rs, 1+
    repeat
    drop
;

\ Store N bytes from ADDR to the dictionary.
: s, ( addr n -- )
    here over allot swap move ;

\ Re-store a string in the dictionary.
: string, ( addr n -- new-addr n )
    here -rot tuck s, ;

: s"
    \ Emit a branch before the string, we could be in a definition.
    if-compiling
        forward-branch
    endif
    read-string
    if-compiling
        string,
        rot here patch-forward-branch
        swap
        [compile] literal
        [compile] literal
    endif
; immediate


: blank ( c-addr u - )
    32 fill ;


\ Count the number of spaces from ADDR backward.
: /string ( caddr1 u1 n - caddr2 u2 )
    tuck - >r + r> ;

: -trailing ( caddr u1 - caddr u2 )
    begin 2dup 1- + c@ 32 = over 0<> and while 1- repeat ;

: compare-integer ( m n -- p )
    2dup < if
        2drop -1
    else
        > if 1 else 0 then
    then
;

: compare ( caddr1 u1 caddr2 u2 -- n )
    rot swap 2dup min -rot >r >r
    0 ?do
        ( caddr1 cadddr2 )
        over i + c@
        over i + c@
        compare-integer case
            -1 of 2drop unloop r> r> 2drop -1 exit endof
             1 of 2drop unloop r> r> 2drop  1 exit endof
             0 of endof
        endcase
    loop
    \ A string is included in the other, so we compare the lengths.
    2drop r> r> compare-integer
;

: string= ( caddr1 u1 caddr2 u2 -- flag )
    compare 0= ;

: string<> string= not ;

\ Check if caddr2 u2 is a substring of caddr1 u1.
: string-prefix? ( caddr1 u1 caddr2 u2 -- f )
    rot
    2dup > if
        2drop 2drop false
    else
        drop dup -rot string=
    endif
;

\ string.fs ends here
