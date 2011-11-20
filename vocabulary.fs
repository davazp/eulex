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

require @structures.fs

: get-current current @ ;
: set-current current ! ;

: previous
    sorder_tos @ 1 >= if sorder_tos 1-! then ;

: definitions
    context @ current ! ;

: wordlist ( -- wid)
    here  0 , ;

: also
    sorder_tos @ sorder_size < if
        context @
        sorder_tos 1+!
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


\ In order to implement VOCS word, we need a kind of introspection for
\ vocabularies. This is provided storing a single-linked list of the
\ available vocabularies in the system.

variable vocentry-root

struct
    1 cells field vocentry-previous
    1 cells field vocentry-size
    1 cells field vocentry-wid
    0 cells field vocentry-name
end-struct vocentry%

: ,vocentry
    vocentry-root @ , dup , 0 , s, ;

: add-vocentry
    here -rot ,vocentry vocentry-root ! ;

: set-last-vocentry-wid ( wid -- )
    vocentry-root @ vocentry-wid ! ;

: vocentry>name ( vc -- addr n )
    dup vocentry-name swap vocentry-size @ ;

: create-vocabulary ( -- wid )
    create here 0 , does> context ! ;

: vocabulary
    create-vocabulary
    latest nt>name add-vocentry
    set-last-vocentry-wid ;

\ vocabulary.fs ends here
