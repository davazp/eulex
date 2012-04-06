\ console.fs --

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

require @kernel/video.fs

variable cursor-x
variable cursor-y

variable color-attr

: attr  color-attr @ ;
: attr! color-attr ! ;

: newline? ( ch -- flag )
    case
        10 of true endof
        13 of true endof
        false swap
    endcase ;

: last-line
    video-height 1- ;

: at-end-on-line?
    cursor-x @ video-width = ;

: at-last-line?
    cursor-y @ video-height = ;

: update-hardware-cursor
    cursor-y @
    cursor-x @
    v-cursor-set-position ;

: clear-char ( i j -- )
    2dup
    32 -rot v-glyph!
    attr -rot v-attr! ;

: clear-line ( i -- )
    video-width 0 ?do
        dup i clear-char
    loop
    drop ;

: clear-last-line
    last-line clear-line ;

: scroll-one-line
    1 0 v-offset                   \ from
    0 0 v-offset                   \ to
    video-memsize video-width 2* - \ bytes
    move
    clear-last-line ;

: emit-newline
    cursor-x 0!
    cursor-y 1+! ;


: emit-char ( ch -- )
    dup newline? if
        drop
        emit-newline
    else
        cursor-y @ cursor-x @ v-glyph!
        attr cursor-y @ cursor-x @ v-attr!
        cursor-x 1+!
        at-end-on-line? if emit-newline endif
    endif ;


: scroll-if-required
    at-last-line? if
        cursor-x 0!
        video-height 1- cursor-y !
        scroll-one-line
    then ;

: emit ( ch -- )
    emit-char
    scroll-if-required
    update-hardware-cursor ;

: at-xy ( column row )
    cursor-y !
    cursor-x !
    update-hardware-cursor ;

: at-beginning
    0 0 at-xy ;

: at-beginning-of-line
    0 cursor-y @ at-xy ;

: at-end
    video-width  1-
    video-height 1-
    at-xy ;

: page
    video-height 0 ?do
        i clear-line
    loop
    at-beginning ;


: invert-screen
    video-width 0 ?do
        video-height 0 ?do
            i j v-attr@ invert i j v-attr!
        loop
    loop ;

PAGE

\ console.fs ends here
