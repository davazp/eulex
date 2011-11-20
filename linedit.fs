\ linedit.fs -- Text line editor

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

\ TODO: Do it extensible!

require @string.fs
require @kernel/console.fs
require @kernel/keyboard.fs

true value visible-bell

variable buffer
variable buffer-size
variable gap-start
variable gap-end

variable finishp

: alert visible-bell if flash else beep endif ;

: point gap-start @ ;

: char-at ( n -- ) buffer @ + c@ ;
: at-beginning? gap-start @ 0= ;
: at-end? gap-end @ buffer-size @ = ;
: full? gap-start @ gap-end @ = ;

: previous-char gap-start @ 1- char-at ;
: next-char gap-end @ char-at  ;

: before-space? at-end? not next-char 32 = and ;
: after-space? at-beginning? not previous-char 32 = and ;

: before-nonspace? at-end? not next-char 32 <> and ;
: after-nonspace?  at-beginning? not previous-char 32 <> and ;

: at-end-word?
    after-nonspace?
    before-space? at-end? or
    and ;

\ Internal words

variable screen-x
variable screen-y

: remember-location
    cursor-x @ screen-x !
    cursor-y @ screen-y ! ;

: restore-location
    screen-x @ cursor-x !
    screen-y @ cursor-y ! ;

: setup-gap-buffer
    dup buffer-size !
    ( ) gap-end !
    buffer !
    gap-start 0! ;

: clear-current-line
    video-width screen-x @ ?do
        screen-y @ i clear-char
    loop ;


\ Editing commands

: le-insert ( ch -- )
    full? if
        alert
    else
        ( ch ) gap-start @ buffer @ + c!
        gap-start 1+!
    endif
;

: le-delete-char
    at-end? if alert else gap-end 1+! endif ;

: le-delete-backward-char
    at-beginning? if alert else gap-start 1-! endif ;

: le-forward-char
    at-end? if
        alert
    else
        gap-end @ buffer @ + c@
        gap-start @ buffer @ + c!
        gap-start 1+!
        gap-end   1+!
    endif
;

: le-backward-char
    at-beginning? if
        alert
    else
        gap-start 1-!
        gap-end   1-!
        buffer @ gap-start @ + c@
        buffer @ gap-end   @ + c!
    endif
;

: le-forward-word
    begin before-space? while le-forward-char repeat
    begin before-nonspace? while le-forward-char repeat ;

: le-backward-word
    begin after-space? while le-backward-char repeat
    begin after-nonspace? while le-backward-char repeat ;

: le-delete-word
    begin before-space? while le-delete-char repeat
    begin before-nonspace? while le-delete-char repeat ;

: le-delete-backward-word
    begin after-space? while le-delete-backward-char repeat
    begin after-nonspace? while le-delete-backward-char repeat ;

: le-move-beginning-of-line
    begin at-beginning? not while le-backward-char repeat ;

: le-move-end-of-line
    begin at-end? not while le-forward-char repeat ;

: le-kill-line
    buffer-size @ gap-end ! ;

: le-return
    at-end? if finishp on else le-move-end-of-line endif ;

: le-clear
    0 0 at-xy
    remember-location
    page ;


\ Autocompletion

\ First of all, we provide two useful twords, INITIALIZE-CORDER and
\ NEXT-WORD, which the iteration across the avalaible words relies on.
\ So, the completion code accesss to the words in a linear and easy
\ way.  Then, implement autocompletion is as simple as record some
\ screen settings and filter the words.

\ This array contains a parallel search order.
create corder-stack sorder_size cells allot
variable corder-tos
variable corder-nt

\ Copy the search order stack to the completion order stack.
: initialize-corder-nt
    context @ wid>latest corder-nt ! ;
: initialize-corder-tos
    sorder_tos @ corder-tos ! ;
: initialize-corder-stack
    get-order 0 ?do
        corder-stack i cells + !
    loop
;
\ Push the address of the of the completion order.
: ccontext ( -- wid )
    corder-tos @ cells
    corder-stack + ;

: next-ccontext ( -- flag )
    corder-tos @ 0 >= if
        corder-tos 1-!
        ccontext @ wid>latest corder-nt !
        true
    else
        false
    endif
;

\ INITIALIZE-CORDER inits the completion search.  It must be called
\ before NEXT-WORD. After that, every call to NEXT-WORD will return
\ the next word avalaible and so, until it returns 0, which indicates
\ that there is not more accessible words.

: initialize-corder ( -- )
    initialize-corder-nt
    initialize-corder-tos
    initialize-corder-stack ;

: next-word ( -- nt|0 )
    corder-nt @ ?dup if
        dup previous-word corder-nt !
    else
        next-ccontext if recurse else 0 endif
    endif
;


\ PREFIX-ADDR and PREFIX-SIZE variables contain the address of the
\ string to be completed and the size respectively.
variable prefix-addr
variable prefix-size
\ Size of the extra size in a completion
variable subfix-size
\ It is TRUE if we are completing a word and it is not the first
\ one. So, if other completion arises, it will share the same prefix.
variable completing?

: setup-prefix ( addr n -- )
    prefix-size ! prefix-addr ! ;

: prefix prefix-addr @ prefix-size @ ;

: word-at-point ( -- addr n )
    \ Note: we are assuming that the AT-END-WORD? is true.
    le-backward-word
    point buffer @ +
    le-forward-word
    point buffer @ +
    over - ;

: next-match ( -- addr n )
    begin
    next-word ?dup while
        dup nt>name prefix string-prefix? if
            nt>name exit
        else
            drop
        endif
    repeat
    0
;

\ Delete the added characteres by the last completion.
: delete-subfix
    subfix-size @ 0 ?do le-delete-backward-char loop ;

\ Skip the prefixed characters of a completion.
: skip-prefix ( addr n -- addr+PREFIX-SIZE n-PREFIX-SIZE )
    prefix-size @ - swap
    prefix-size @ + swap ;

: insert-string ( addr n -- )
    0 ?do dup c@ le-insert 1+ loop drop ;

: complete-word
    delete-subfix
    next-match ?dup if
        skip-prefix dup subfix-size !
        insert-string
    else
        completing? off
    endif
;

: le-complete-word
    at-end-word? if
        completing? @ not if
            initialize-corder
            word-at-point setup-prefix
            subfix-size 0!
            completing? on
        endif
        complete-word
    endif
;


\ Initialization and finalization

: init ( buffer size -- )
    false finishp !
    remember-location
    setup-gap-buffer ;

: finalize ( -- c)
    le-move-end-of-line
    gap-start @ ;


\ Rendering

: render-pre-cursor
    gap-start @ 0 ?do
        buffer @ i + @ emit-char
    loop ;

: render-post-cursor
    buffer-size @ gap-end @ ?do
        buffer @ i + @ emit-char
    loop ;

: render ( -- )
    clear-current-line
    restore-location
    render-pre-cursor
    update-hardware-cursor
    render-post-cursor ;

\ Looping

: non-special-key? $80 <= ;

: alt-dispatcher
    case
        [char] f of le-forward-word endof
        [char] b of le-backward-word endof
        [char] d of le-delete-word endof
        BACK     of le-delete-backward-word endof
    endcase
;
: ctrl-dispatcher
    case
        [char] a of le-move-beginning-of-line  endof
        [char] e of le-move-end-of-line        endof
        [char] f of le-forward-char            endof
        [char] b of le-backward-char           endof
        [char] d of le-delete-char             endof
        [char] k of le-kill-line               endof
        [char] l of le-clear                   endof
    endcase
;
: command-dispatcher ( key modifiers -- )
    over TAB = if
        2drop le-complete-word
        exit
    else
        completing? off
    endif
    dup alt-mod = if
        drop
        alt-dispatcher
        exit
    endif
    dup ctrl-mod = if
        drop
        ctrl-dispatcher
        exit
    endif
    dup ctrl-mod alt-mod or = if
        2drop
        exit
    endif
    over RET = if
        2drop
        le-return
        exit
    endif
    over non-special-key? if
        drop le-insert
        exit
    endif
    over BACK = if
        2drop le-delete-backward-char
        exit
    endif
    over LEFT = if
        2drop le-backward-char
        exit
    endif
    over RIGHT = if
        2drop le-forward-char
        exit
    endif
    2drop
;

: looping ( -- )
    begin
        render
        ekey
        command-dispatcher
    finishp @ until ;


\ High level words

: edit-line ( addr n1 n2 -- n3 )
    >r init r> gap-start !
    looping
    finalize ;

: accept ( addr n1 -- n2 )
    0 edit-line ;

\ linedit.fs ends here
