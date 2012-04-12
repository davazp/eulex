\ editor.fs --- Real-time display block editor

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

VOCABULARY EDITOR
VOCABULARY EDITOR-COMMANDS
EULEX ALSO EDITOR DEFINITIONS

require @kernel/console.fs
require @colors.fs
require @blocks.fs

variable buffer
variable point

true value visible-bell
: flash invert-screen 50 ms invert-screen ;
: alert visible-bell if flash else beep endif ;

: line point @ 64 / ;
: column point @ 64 mod ;

: point>screen ( position -- x y )
    64 /mod 4 + swap 7 + swap ;

: update-cursor point @ point>screen at-xy update-hardware-cursor ;
: render-text-line 64 * dup point>screen at-xy buffer @ + 64 type ;
: render-text buffer @ 16 0 ?do 07 04 I + at-xy dup 64 type 64 + loop drop ;

: box-corners
    06 03 at-xy $da emit-char
    71 03 at-xy $bf emit-char
    06 20 at-xy $c0 emit-char
    71 20 at-xy $d9 emit-char ;

: --- 0 ?do $c4 emit-char loop ;
: | $b3 emit-char ;
: .2 dup 10 < if $20 emit-char then . ;

: box
    gray upon black
    07 03 at-xy 64 ---
    16 00 ?do cr 3 spaces i .2 | 64 spaces | loop
    07 20 at-xy 64 ---
    box-corners ;

: redraw
    upon blue 80 spaces
    36 0 at-xy light cyan ." EDITOR "
    box
    00 23 at-xy upon blue 80 spaces
    light gray upon black render-text
    update-cursor ;

: insert-literally ( ch -- )
    point @ 1023 = if alert else
        buffer @ point @ + c! point 1+!
    endif ;

: insert ( ch -- )
    dup [char] a = if alert then
    dup 10 = if
        64 column - 0 ?do 32 insert-literally loop
        render-text
    else
        insert-literally
    endif ;

variable editor-loop-quit
: editor-loop
    editor-loop-quit off
    begin
        ekey drop case
            ESC of editor-loop-quit on endof
            insert
            line render-text-line
        endcase
        update-cursor
    editor-loop-quit @ until ;

: edit ( u -- )
    block buffer !
    save-screen
    page redraw
    editor-loop
    restore-screen ;

' EDIT
PREVIOUS DEFINITIONS ALIAS EDIT

\ editor.fs ends here
