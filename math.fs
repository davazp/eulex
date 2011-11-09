\ math.fs --  Mathematical words

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

: gcd ( m n -- gcd[m,n] )
    begin
    dup 0<> while
        tuck mod
    repeat
    drop
;

: lcm ( m n -- lcm[m,n] )
    2dup * -rot gcd / ;


: divisible ( m n -- flag )
    mod 0= ;

: fact ( n -- n! )
    1 swap
    0 ?do
        i 1+ *
    loop ;

\ math.fs ends here
