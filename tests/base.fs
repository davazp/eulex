\ base.fs --

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

Checking simple arithmetics operations...
2 2 + 4 = assert
0 1 + 1 = assert

Checking arithmetic carry and overflow...
\ Positive tests
:noname  500000000000  500000000000 + OF? nip assert ; execute
:noname -500000000000 -500000000000 + OF? nip assert ; execute
:noname  500000000000 2 * OF? nip assert ; execute
:noname -500000000000 2 * OF? nip assert ; execute
:noname -1  1 + CF? nip assert ; execute
:noname  1 -1 + CF? nip assert ; execute
\ Negative tests
:noname 0 0  + OF? nip noassert ; execute
:noname 0 0  - OF? nip noassert ; execute
:noname 0 0  + CF? nip noassert ; execute
:noname 0 0  - CF? nip noassert ; execute
:noname -2 1 + CF? nip noassert ; execute
:noname 100000000 100000000 + OF? nip noassert ; execute

Checking simple logical operations...
true true  and true  = assert
true false and false = assert

Checking 0-9 ascii consistency...
char 0
   dup char 0 = assert
1+ dup char 1 = assert
1+ dup char 2 = assert
1+ dup char 3 = assert
1+ dup char 4 = assert
1+ dup char 5 = assert
1+ dup char 6 = assert
1+ dup char 7 = assert
1+ dup char 8 = assert
1+ dup char 9 = assert
drop

Checking A-F ascii consistency...
char A
   dup char A = assert
1+ dup char B = assert
1+ dup char C = assert
1+ dup char D = assert
1+ dup char E = assert
1+ dup char F = assert
drop

Checking a-f ascii consistency...
char a
   dup char a = assert
1+ dup char b = assert
1+ dup char c = assert
1+ dup char d = assert
1+ dup char e = assert
1+ dup char f = assert
drop

Checking simple stack words...
clearstack depth 0= assert

Checking bit word...
 0 0 bit? false = assert
 1 0 bit? true  = assert
10 0 bit? false = assert
10 1 bit? true  = assert
10 2 bit? false = assert
10 3 bit? true  = assert

Checking CASE word...
: test-case ( n )
    case
        1 of 1000 endof
        2 of 2000 endof
        3 of 3000 endof
        \ Default
        12345 swap
    endcase
;
1 test-case 1000  = assert
2 test-case 2000  = assert
3 test-case 3000  = assert
0 test-case 12345 = assert

Checking some math words...
0  fact       1 = assert
1  fact       1 = assert
2  fact       2 = assert
3  fact       6 = assert
4  fact      24 = assert
5  fact     120 = assert
10 fact 3628800 = assert

 0 0 gcd 0 = assert
 0 1 gcd 1 = assert
 1 0 gcd 1 = assert
12 8 gcd 4 = assert

 1 1 lcm  1 = assert
 2 3 lcm  6 = assert
 3 2 lcm  6 = assert
12 6 lcm 12 = assert
12 8 lcm 24 = assert


Checking recurse...
: fact2 ( n -- n! )
    dup 0> if
        dup 1- recurse *
    else
        drop 1
    endif ;

0 fact2   1 = assert
1 fact2   1 = assert
2 fact2   2 = assert
3 fact2   6 = assert
4 fact2  24 = assert
5 fact2 120 = assert


Checking return stack operations...

:noname
    0
    >r r>
    0= assert
; execute

:noname
    0 >r
    r@ 0= assert
    r> drop
; execute

:noname
    rsp
    0 >r r> drop
    rsp
    = assert
; execute

:noname
    0 1
    2>r 2r>
    1 = assert
    0 = assert
; execute

:noname
    0 1 2>r
    r> 1 = assert
    r> 0 = assert
; execute


Checking do-loop....
: foo 5 0 ?do i loop ;
foo depth 5 = assert
clearstack
: foo 0 0 ?do i loop ;
foo depth 0 = assert
clearstack
: foo 100 0 ?do i leave loop ;
foo depth 1 = assert
clearstack
: foo
    100 0 ?do
        i 9 = if
            i unloop exit
        else
            i
        endif
    loop
;
foo depth 10 = assert
clearstack

Checking the stack is empty...
depth 0= assert

\ base.fs ends here
