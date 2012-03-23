\ lisp.fs --- A straighforward dynamically-scoped Lisp

\ Copyright 2012 (C) David Vazquez

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

VOCABULARY LISP
ALSO LISP DEFINITIONS

3 constant tag-bits
1 tag-bits lshift 1 - constant tag-mask

%000 constant even-fixnum-tag
%100 constant odd-fixnum-tag
%001 constant cons-tag
%011 constant symbol-tag

: allocate-cons ( x y -- xy )
    2 cells allocate throw cons-tag or ;

: >fixnum [ tag-bits 1 - ]L lshift ;
: fixnum> [ tag-bits 1 - ]L rshift ;

: integer? 1 and 0= ;
: cons? tag-mask and cons-tag = ;
: symbol? tag-mask and symbol-tag = ;

: tagged ( x tag -- x* )
    swap tag-bits lshift or ;

\ Like tagged, but check for x!=0.
: ?tagged ( x tag -- x* )
    swap dup 0= if nip else swap tagged then ;

vocabulary lisp-package

: in-lisp-package:
    only eulex lisp-package ;

: ?nt>pfa dup if nt>pfa then ;

: find-symbol ( c-addr -- symbol|0 )
    >r get-order r>
    in-lisp-package: find-cname
    >r set-order r>
    ?nt>pfa aligned symbol-tag ?tagged ;

: create-cname ( c-addr -- )
    count nextname header reveal ;

: intern-symbol ( c-addr -- symbol )
    dup find-symbol ?dup if nip else
        >r get-order get-current in-lisp-package: definitions r>
        create-cname set-current set-order
        align here 2 cells allot symbol-tag tagged         
    then ;


PREVIOUS DEFINITIONS

\ Provide RUN-LISP in the system vocabulary
LATESTXT ALIAS RUN-LISP

\ lisp.fs ends here
