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
variable &point

: point &point @ ;
: point! &point ! ;

true value visible-bell
: flash invert-screen 50 ms invert-screen ;
: alert visible-bell if flash else beep endif ;

: line point 64 / ;
: column point 64 mod ;
: right-column 63 column - ;
: beginning-of-line point column - ;
: end-of-line beginning-of-line 63 + ;

: offset>addr chars buffer @ + ;
: char-at offset>addr c@ ;

: point>addr point offset>addr ;

: offset>screen ( position -- x y )
    64 /mod 4 + swap 7 + swap ;

: update-cursor point offset>screen at-xy update-hardware-cursor ;

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

: render-application
    page upon blue 80 spaces
    36 0 at-xy light cyan ." EDITOR "
    box
    00 23 at-xy upon blue 80 spaces
    light gray upon black ;

\ Bitmap of lines which need to be redrawn
variable lines-to-render
: render-line
    64 * dup offset>screen at-xy offset>addr 64 type ;
: render
    lines-to-render @
    16 0 ?do dup 1 and if i render-line endif 1 rshift loop
    lines-to-render ! ;
: redraw-line 1 line lshift lines-to-render or! ;
: redraw-buffer -1 lines-to-render ! ;

\ Commands

variable editor-loop-quit
variable last-read-key
create keymap 1024 cells zallot

: ekey->kbd
    dup alt-mod and if swap $100 + swap then
    ctrl-mod and if $200 + then ;

: read-key ekey ekey->kbd dup last-read-key ! ;

: kbd-command cells keymap + @ ;

: in-range? 0 swap 1024 between ;
: move-char ( n -- )
    point swap &point +! point in-range? if drop else
        point! abort
    then ;

: empty-line? ( u -- bool )
    64 * dup 64 + swap ?do
        i char-at 32 <> if unloop false exit then
    loop true ;

: last-char-is-empty?
    end-of-line char-at 32 = ;

: memshift> ( addr u1 u2 -- )
    swap >r 2dup + swap r> - abs cmove> ;
: <memshift ( addr u1 u2 -- )
    tuck - abs >r over + swap r> cmove ;

: insert-char-literally ( ch -- )
    last-char-is-empty? not if abort then
    point>addr right-column 1+ 1 memshift>
    point>addr c! 1 move-char ;

: insert-newline
    15 empty-line? not if abort then
    end-of-line 1+ dup offset>addr swap 1024 swap - 64 memshift>
    end-of-line 1+ offset>addr 64 32 fill
    point>addr right-column 1+ 64 + right-column 1+ memshift>
    point>addr right-column 1+ 32 fill
    end-of-line 1+ &point !
    redraw-buffer ;

: insert-char ( ch -- )
    dup 10 = if drop insert-newline else insert-char-literally endif ;


ALSO EDITOR-COMMANDS DEFINITIONS

: forward-char 1 move-char ;
: backward-char -1 move-char ;
: next-line 64 move-char ;
: previous-line -64 move-char ;

: self-insert-command
    redraw-line last-read-key @ insert-char redraw-line ;

: delete-char
    point>addr right-column 1+ 1 <memshift
    32 end-of-line offset>addr c!
    redraw-line ;

: delete-backward-char
    column 0= if abort then
    backward-char delete-char ;

: kill-editor
    editor-loop-quit on ;


ALSO EDITOR DEFINITIONS
: M- char $100 + ; : C- char $200 + ;
: key-for: nt' swap cells keymap + ! ;

ESC   key-for: kill-editor
UP    key-for: previous-line
DOWN  key-for: next-line
LEFT  key-for: backward-char
RIGHT key-for: forward-char
BACK  key-for: delete-backward-char

C- f  key-for: forward-char
C- b  key-for: backward-char
C- p  key-for: previous-line
C- n  key-for: next-line
C- d  key-for: delete-char

:noname
    127 32 ?do [nt'] self-insert-command i cells keymap + ! loop
; execute

PREVIOUS EDITOR-COMMANDS
PREVIOUS EDITOR
DEFINITIONS

\ Command dispatch

: editor-loop
    editor-loop-quit off
    begin
        render update-cursor
        read-key kbd-command ?dup if
            nt>xt ['] execute catch if 2drop alert then
        then
    editor-loop-quit @ until ;

: edit ( u -- )
    0 point! block buffer !
    save-screen
    render-application redraw-buffer
    editor-loop
    restore-screen ;

' EDIT
PREVIOUS DEFINITIONS ALIAS EDIT

\ editor.fs ends here
