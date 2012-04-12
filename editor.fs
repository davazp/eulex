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

variable prefix

: N prefix @ ;                  : N! prefix ! ;
: -N! N negate N! ;             : *N! N * N! ;
: 1N! 1 N! ;                    : N# N 1N! ;

: in-range? 0 swap 1024 between ;

: forward-char
    point N &point +! point in-range? if drop else point! abort then ;

: backward-char -N! forward-char ;
: next-line 64 *N! forward-char ;
: previous-line -N! next-line ;

: last-char-is-empty?
    end-of-line char-at 32 = ;

: last-line-is-empty?
    64 0 ?do
        i 960 + char-at 32 <> if unloop false exit then
    loop true ;

\ Shift each byte in the region ADDR U1, U2 places to the right. U2
\ bytes are lost.
: memshift> ( addr u1 u2 -- )
    swap >r 2dup + swap r> swap - cmove> ;

: insert-literally ( ch -- )
    N# 0 ?do
        last-char-is-empty? not if abort then
        point>addr right-column 1+ 1 memshift>
        point>addr c! forward-char
    loop ;

: insert-newline
    N# 0 ?do
        last-line-is-empty? not if abort then
        end-of-line 1+ dup offset>addr swap 1024 swap - 64 memshift>
        end-of-line 1+ offset>addr 64 32 fill
        point>addr right-column 1+ 64 + right-column 1+ memshift>
        point>addr right-column 1+ 32 fill
        right-column 1+ N! forward-char
    loop
    redraw-buffer ;

: insert ( ch -- )
    dup 10 = if drop insert-newline else insert-literally endif ;


variable editor-loop-quit
: command-dispatch
    1N! drop case
        ESC   of editor-loop-quit on endof
        UP    of previous-line endof
        DOWN  of next-line endof
        LEFT  of backward-char endof
        RIGHT of forward-char endof
        redraw-line dup insert redraw-line
    endcase ;

: editor-loop
    editor-loop-quit off
    begin
        render update-cursor
        ekey ['] command-dispatch catch if 2drop alert then
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
