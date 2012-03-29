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

\ Low-level search-order manipulation

struct
    cell field wid-latest
    cell field wid-method
end-struct wid%

: wid>latest wid-latest @ ;

: context
    sorder_stack sorder_tos @ cells + ;

: forth-impl
    [ context @ ]L context ! ;

: get-order ( -- widn .. wid1 n )
    sorder_stack
    sorder_tos @ 1+ 0 ?do
        dup @ swap cell +
    loop
    drop
    sorder_tos @ 1+
;

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
    then
;

: get-current current @ ;
: set-current current ! ;

: previous
    sorder_tos @ 1 >= if sorder_tos 1-! then ;

: definitions
    context @ current ! ;

: wordlist ( -- wid)
    here wid% zallot ;

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
    create wordlist does> context ! ;

: vocabulary
    create-vocabulary
    latest nt>name add-vocentry
    set-last-vocentry-wid ;

\ Define Forth and Root vocabularies

wordlist constant forth-wordlist
: Forth forth-wordlist context ! ;
latest nt>name add-vocentry
forth-wordlist set-last-vocentry-wid

wordlist constant root-wordlist
: Root root-wordlist >order ;
latest nt>name add-vocentry
root-wordlist set-last-vocentry-wid

: Eulex forth-impl ;
latest nt>name add-vocentry
context @ set-last-vocentry-wid

: only
    sorder_tos 0!
    root-wordlist context !
    also ;

Root definitions
' set-order alias set-order
' forth-wordlist alias forth-wordlist
' eulex alias eulex
' forth alias forth
previous definitions

\ vocabulary.fs ends here
