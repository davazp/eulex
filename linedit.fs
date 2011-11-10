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

variable screen-x
variable screen-y

variable buffer
variable buffer-size
variable gap-start
variable gap-end

variable finishp

: alert visible-bell if flash else beep endif ;

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
    at-end? if
        true finishp !
    else
        le-move-end-of-line
    endif ;


\ Internal words

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
    endcase
;
: command-dispatcher ( key modifiers -- )
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
