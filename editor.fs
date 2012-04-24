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

\ TODO: Mark commands with a special flag to allow ordinary Forth
\ words in the EDITOR-CMDS vocabulary.

VOCABULARY EDITOR
VOCABULARY EDITOR-CMDS

EULEX
ALSO EDITOR
ALSO EDITOR DEFINITIONS

require @kernel/console.fs
require @colors.fs
require @blocks.fs

variable nblock
variable buffer
variable &point

: memshift> ( addr u c -- ) swap >r 2dup + swap r> - abs cmove> ;
: <memshift ( addr u c -- ) tuck - abs >r over + swap r> cmove ;

: point &point @ ;
: goto-char &point ! ;

true value visible-bell
: flash invert-screen 50 ms invert-screen ;
: alert visible-bell if flash else beep endif ;

: line-number-at-pos 64 / ;
: column-number-at-pos 64 mod ;
: line point line-number-at-pos ;
: column point column-number-at-pos ;
: right-column 63 column - ;
: line-beginning-position point column - ;
: line-end-position line-beginning-position 63 + ;
: position>addr chars buffer @ + ;
: char-at position>addr c@ ;
: point>addr point position>addr ;

: position>screen 64 /mod 4 + swap 7 + swap ;
: update-cursor point position>screen at-xy update-hardware-cursor ;

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

: render-title 
    page upon blue 80 spaces
    36 0 at-xy light cyan ." EDITOR " ;

create minibuffer-string 79 chars allot
: render-modeline
    attr
    00 23 at-xy upon blue 80 spaces
    03 23 at-xy light cyan ." Block: " nblock ?
    attr! ;

: render-minibuffer
    00 24 at-xy light gray upon black minibuffer-string 79 type ;

: render-application
    render-title box render-modeline render-minibuffer ;

\ Bitmap of lines which need to be redrawn
variable lines-to-render
: safe-emit dup 32 < if drop [char] . then emit ;
: safe-type 0 ?do dup c@ safe-emit 1+ loop drop ;
: render-line
    64 * dup position>screen at-xy position>addr 64 safe-type ;
: render
    lines-to-render @
    16 0 ?do dup 1 and if i render-line endif 1 rshift loop
    lines-to-render !
    render-minibuffer ;
: redraw-line 1 line lshift lines-to-render or! ;
: redraw-buffer -1 lines-to-render ! ;
: redraw-lines ( from to -- )
    1 swap 1+ lshift 1- swap
    1 swap    lshift 1- negate .s
    lines-to-render or! ;
    
create command-name 80 chars allot
: in-editor-cmds: also editor-cmds context @ 1 set-order ;
: read-command
    get-order in-editor-cmds:
    0 24 at-xy ." M-x " command-name dup 74 accept
    c-addr find-cname >r
    set-order r> ;

\ Commands

variable editor-loop-quit
variable last-read-key
create keymap 1024 cells zallot

: out-of-range? 0 swap 1023 between not ;

: at-beginning? point 0 = ;
: at-end? point 1023 = ;

: move-char ( n -- )
    point + dup out-of-range? if abort else goto-char then ;

: white-string? -trailing nip 0= ;
: empty-line? ( u -- bool )
    960 position>addr 64 white-string? ;

: shift> ( addr u c -- )
    rot dup >r -rot dup >r memshift> r> r>  swap 32 fill ;
: <shift ( addr u c -- )
    2 pick 2 pick + over - >r dup >r <memshift r> r> swap 32 fill ;

: [INTERNAL] also editor definitions ;
: [END] previous definitions ;

' point alias <E
' goto-char alias E>

: message ( addr u -- )
    minibuffer-string 79 32 fill
    79 min minibuffer-string swap move ;

: clear-minibuffer
    0 0 message ;

: substring ( position1 position2 -- )
    over - >r position>addr r> ;

: open-buffer ( u -- )
    dup nblock !
    block buffer !
    redraw-buffer render-modeline ;

EDITOR-CMDS DEFINITIONS

: beginning-of-line line-beginning-position goto-char ;
' beginning-of-line alias bol
: end-of-line line-end-position goto-char ;
' end-of-line alias eol

[INTERNAL]
: whole-line <E bol point eol point substring 1+ rot E> ;
: rest-of-line <E dup eol point substring 1+ rot E> ;
[END]

: rewind
    rest-of-line white-string? if
        begin point char-at 32 = column 0<> and while -1 move-char repeat
        point char-at 32 <> if 1 move-char then
    then ;

: next-line 64 move-char rewind ;
: previous-line -64 move-char rewind ;

: forward-char
    rest-of-line tuck white-string? not if drop 1 then
    move-char ;

: backward-char
    column 0= if previous-line eol rewind else -1 move-char endif ;

: beginning-of-buffer 0 goto-char ;     : end-of-buffer 1023 goto-char ;
' beginning-of-buffer alias bob         ' end-of-buffer alias eob

: beginning-of-paragraph
    bol begin at-beginning? if exit then
    point 1- char-at 32 <> while
        previous-line
    repeat ;
' beginning-of-paragraph alias bop

: end-of-paragraph
    eol begin point char-at 32 <> at-end? not and while
        next-line
    repeat
    rewind ;
' end-of-paragraph alias eop

: forward-word
    begin point char-at 32  = at-end? not and while 1 move-char repeat
    begin point char-at 32 <> at-end? not and while 1 move-char repeat ;

: backward-word
    at-beginning? not if -1 move-char then
    begin point char-at 32  = at-beginning? not and while -1 move-char repeat
    begin point char-at 32 <> at-beginning? not and while -1 move-char repeat
    at-beginning? not if 1 move-char then ;

[INTERNAL]
: whole-paragraph <E bop point eop point substring 1+ rot E> ;
: whole-buffer    <E bob point eob point substring 1+ rot E> ;

: rest-of-paragraph <E dup eop point substring 1+ rot E> ;
: rest-of-buffer    <E dup eob point substring 1+ rot E> ;
[END]

: erase-buffer whole-buffer 32 fill bob ;

: newline
    15 empty-line? not if abort then
    <E next-line bol rest-of-buffer 64 shift> E>
    point>addr right-column 65 + right-column 1+ shift>
    eol 1 move-char redraw-buffer ;

: self-insert-command
    rest-of-paragraph 1 memshift>
    last-read-key @ point>addr c!
    1 move-char redraw-buffer ;

: delete-char rest-of-paragraph 1 <shift redraw-buffer ;
: delete-backward-char backward-char delete-char ;

: execute-extended-command
    read-command ?dup if nt>xt execute else abort then ;

: execute-buffer
;

: save-buffer
    update flush s" Block changes saved." message ;

: next-buffer nblock @ 1 + open-buffer ;
: previous-buffer nblock @ ?dup if 1- open-buffer then ;

: kill-editor editor-loop-quit on ;


ALSO EDITOR DEFINITIONS

: ekey->kbd
    dup alt-mod and if swap $100 + swap then
    ctrl-mod and if $200 + then ;

: read-key ekey ekey->kbd dup last-read-key ! ;
: kbd-command cells keymap + @ ;

: M-   char $100 + ;
: C-   char $200 + ;
: C-M- char $300 + ;
: key-for: nt' swap cells keymap + ! ;

: C-X-dispatcher
    read-key case
        [ C- s ]L of save-buffer endof
        [ C- c ]L of kill-editor endof
        abort
    endcase ;

UP    key-for: previous-line
DOWN  key-for: next-line
LEFT  key-for: backward-char
RIGHT key-for: forward-char
BACK  key-for: delete-backward-char
RET   key-for: newline

M- x  key-for: execute-extended-command
M- <  key-for: beginning-of-buffer
M- >  key-for: end-of-buffer
M- f  key-for: forward-word
M- b  key-for: backward-word

C- x  key-for: C-X-dispatcher
C- f  key-for: forward-char
C- b  key-for: backward-char
C- p  key-for: previous-line
C- n  key-for: next-line
C- d  key-for: delete-char
C- a  key-for: beginning-of-paragraph
C- e  key-for: end-of-paragraph
C- c  key-for: execute-buffer


:noname
    127 32 ?do [nt'] self-insert-command i cells keymap + ! loop
; execute

PREVIOUS EDITOR-CMDS
PREVIOUS EDITOR
DEFINITIONS

\ Command dispatch

: editor-loop
    editor-loop-quit off
    begin
        render update-cursor redraw-line
        read-key clear-minibuffer kbd-command ?dup if
            nt>xt ['] execute catch if drop alert then
        else alert then
        redraw-line
    editor-loop-quit @ until ;

: edit ( u -- )
    dup nblock ! block buffer !
    save-screen
    clear-minibuffer
    0 goto-char
    render-application redraw-buffer
    editor-loop
    restore-screen ;

' EDIT
PREVIOUS DEFINITIONS ALIAS EDIT

\ editor.fs ends here
