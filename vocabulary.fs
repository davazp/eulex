\ vocabulary.fs --

\ Copyright 2011, 2012 (C) David Vazquez

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

variable last-wid

struct
    cell field wid-latest
    cell field wid-method
    cell field wid-name
    cell field wid-previous
end-struct wid%

: wid>latest wid-latest @ ;

: wid>name ( wid -- addr n )
    wid-name @ ?dup if count else 0 0 then ;

: context
    sorder_stack sorder_tos @ cells + ;

context @ constant forth-impl-wordlist
: forth-impl
    forth-impl-wordlist context ! ;
forth-impl-wordlist last-wid !

: get-order ( -- widn .. wid1 n )
    sorder_stack
    sorder_tos @ 1+ 0 ?do
        dup @ swap cell +
    loop
    drop
    sorder_tos @ 1+ ;

: set-order ( widn .. wid1 n -- )
    dup 0= if
        sorder_tos 0!
        forth-impl
        drop
    else
        dup 1- sorder_tos !
        context swap
        0 ?do
            dup -rot ! cell -
        loop
        drop
    then ;

: get-current current @ ;
: set-current current ! ;

: previous
    sorder_tos @ 1 >= if sorder_tos 1-! then ;

: definitions
    context @ current ! ;

: allocate-wordlist ( -- wid )
    here wid% zallot ;

: wordlist ( -- wid )
    here allocate-wordlist last-wid @ over wid-previous ! last-wid ! ;

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

\ <wordlist> DOWORDS ... ENDWORDS
\
\ A loop construction to iterate on the words in a wordlist. The body
\ is executed with a NT on the TOS each time. You MUST NOT remove this
\ element from the stack.
: dowords
    postpone wid>latest
    postpone begin
    postpone ?dup
    postpone while
; immediate compile-only
: endwords
    postpone previous-word
    postpone repeat
; immediate compile-only

\ Vocabularies

: vocabulary
    create latest nt>cname wordlist wid-name ! does> context ! ;

\ Define Forth and Root vocabularies
wordlist constant forth-wordlist
wordlist constant root-wordlist

: Forth forth-wordlist context ! ;
: Root root-wordlist >order ;
: Eulex forth-impl ;

nt' Forth nt>cname forth-wordlist      wid-name !
nt' Root  nt>cname root-wordlist       wid-name !
nt' Eulex nt>cname forth-impl-wordlist wid-name !

: only sorder_tos 0! root-wordlist context ! also ;

Root definitions
' set-order alias set-order
' forth-wordlist alias forth-wordlist
' eulex alias eulex
' forth alias forth
previous definitions

\ vocabulary.fs ends here
