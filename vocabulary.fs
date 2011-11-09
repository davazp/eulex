\ vocabulary.fs --

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

: get-current current @ ;
: set-current current ! ;

: previous
    sorder_tos @ 1 >= if
        -1 sorder_tos +!
    then ;

: definitions
    context @
    current ! ;

: wordlist ( -- wid)
    here  0 , ;

: vocabulary
    create 0 , does> context ! ;

: also
    sorder_tos @ sorder_stack < if
        context @
        1 sorder_tos +!
        context !
        \ This is commented because we have not ." in this point.
        \ else
        \ ." ERROR: Too wordlists in the search order stack." cr
    then
;

: >order ( wid -- )
    also context ! ;

: only
    sorder_tos 0!
    forth-impl ;


\ vocabulary.fs ends here
