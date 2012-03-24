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

vocabulary lisp
get-current
also eulex
also lisp definitions

3 constant tag-bits
1 tag-bits lshift 1 - constant tag-mask

%000 constant even-fixnum-tag
%100 constant odd-fixnum-tag
%001 constant cons-tag
%011 constant symbol-tag

: tagged or ;
: ?tagged swap dup 0= if nip else swap tagged then ;
: untag tag-mask invert and ;

\ We write the lisp package system upon wordlists. The PF of the words
\ contains the symbol value and the symbol function parameters aligned
\ to a double cell size.
wordlist constant lisp-package
lisp-package >order

: in-lisp-package: only eulex lisp-package >order ;

: create-in-lisp-package
    get-order get-current in-lisp-package: definitions
    create set-current set-order ;

: find-cname-in-lisp-package ( c-addr -- )
    >r get-order in-lisp-package: r>
    find-cname >r set-order r> ;

: create-symbol
    create-in-lisp-package 2align does> 2aligned symbol-tag tagged ;

: ::unbound [ here 2aligned symbol-tag tagged ]L ;

create-symbol t                 t , ::unbound ,
create-symbol nil             nil , ::unbound ,

: >bool if t else nil then ;
: bool> nil = if 0 else -1 then ;

: eq? = >bool ;

: symbol?
    tag-mask and symbol-tag = >bool ;

\ Check if X is a symbol object. If not, it signals an error.
: check-symbol ( x -- x )
    dup symbol? NIL = if 1 throw then ;

: %find-symbol ( c-addr -- symbol|0 )
    find-cname-in-lisp-package ?dup if nt>xt execute endif ;

: %intern-symbol ( c-addr -- symbol )
    dup %find-symbol ?dup if nip else
        count nextname create-symbol ::unbound , ::unbound ,
        latestxt execute 
    then ;

: symbol-value ( symbol -- value )
    check-symbol untag @
    dup ::unbound = if 2 throw endif ;

: symbol-function ( symbol -- value )
    check-symbol untag cell + @
    dup ::unbound = if 3 throw endif ;

: set ( symb value -- )
    swap check-symbol untag ! ;

: fset ( symbol definition -- )
    swap check-symbol untag cell + ! ;
    



: run-lisp
    page 0 0 at-xy ." RUNNING EULEX LISP." cr ;

previous previous set-current

\ Provide RUN-LISP in the system vocabulary
LATESTXT ALIAS RUN-LISP

\ lisp.fs ends here
