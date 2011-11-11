\ interpreter.fs

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

: octal    8 base ! ;
: decimal 10 base ! ;
: hex     16 base ! ;

\ Directivas

Defer [if]      immediate
Defer [else]    immediate
Defer [then]    immediate


' [then] alias [endif]

: read-word
    parse-cname find-cname dup 0<> if
        nt>xt
    then ;

: lookup-else-or-then-1
    0 >r
    begin
        read-word
        case
            ['] [if]   of r> 1+ >r endof
            ['] [else] of
                r>
                ?dup 0= if
                    exit
                else
                    >r
                then
            endof
            ['] [then] of
                r>
                ?dup 0= if
                    exit
                else
                    1- >r
                then
            endof
        endcase
    again
;

: lookup-else-or-then
    not if lookup-else-or-then-1 endif
; latestxt IS [IF]

: lookup-then
    0 >r
    begin
        read-word
        case
            ['] [if] of r> 1+ >r endof
            ['] [then] of
                r> ?dup 0= if
                    exit
                else
                    1- >r
                then
            endof
        endcase
    again
; latestxt IS [ELSE]

: noop ; latestxt IS [THEN]


: [defined] parse-cname find-cname 0<> ; immediate
: [ifdef]
    postpone [defined]
    postpone [if]
; immediate

: [ifundef]
    postpone [defined]
    not
    postpone [if]
; immediate


\ interpreters.fs ends here
