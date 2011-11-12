\ keyboard.fs ---

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

require @kernel/irq.fs

$5e constant kbd-intrfc
$60 constant kbd-port
$64 constant kbd-io
$fe constant kdb-reset

: clear-kbd-buffer
    begin
        kbd-intrfc inputb
        dup 0 bit? if drop kbd-io inputb then
    1 bit? false = until ;

: kbd-scancode
    kbd-port inputb ;


64 constant kbdbuff-size
create kbdbuff kbdbuff-size allot
variable kbdbuff-wp
variable kbdbuff-rp

: kbdp++ ( ptr -- )
    dup @ 1+ kbdbuff-size mod swap ! ;
: kbdbuff-empty?
    kbdbuff-rp @
    kbdbuff-wp @ = ;
: kbdbuff-full?
    kbdbuff-wp @ 1+ kbdbuff-size mod
    kbdbuff-rp @ = ;

: irq1-keyboard
    \ This word is marked as IRQ handler, and therefore it is called
    \ by the FLIH with interrupts disabled, so that we can grant the
    \ atomicity of this routine.
    kbdbuff-full? if kbdbuff-rp kbdp++ then
    kbd-scancode kbdbuff-wp @ kbdbuff + c!
    kbdbuff-wp kbdp++
; 1 IRQ


: scancode? ( -- flag )
    kbdbuff-empty? not ;

: wait-scancode
    begin scancode? not while halt repeat ;

: discard-scancode
    kbdbuff-rp kbdp++ ;

: peek-scancode ( -- sc )
    wait-scancode
    kbdbuff-rp @ kbdbuff + c@ ;

: scancode ( -- sc )
    peek-scancode
    discard-scancode ;

: flush-scancode
    begin scancode? while discard-scancode repeat ;


\ Scancodes interpretation. SET1 (IBM PC XT)

\ This code is non-extensible and incomplete. We handle very basic
\ keys with alt, shift and ctrl as modifiers. It is enough for me
\ since I will use an emacs-like keybindings. However, feel free to
\ write it!

08 constant TAB
10 constant RET
32 constant ______SPACE______

\ Non-implemented keys.
00 constant CAPSLOCK
00 constant NUMLOCK

$80
enum ESC                enum BACK               enum DEL
enum CTRL               enum SHIFT              enum PRSCR
enum ALT              ( enum CAPSLOCK )         enum F1
enum F2                 enum F3                 enum F4
enum F5                 enum F6                 enum F7
enum F8                 enum F9                 enum F10
enum F11                enum F12              ( enum NUMLOCK )
enum SCRLOCK            enum HOME               enum UP
enum LEFT               enum RIGHT              enum DOWN
enum PGUP               enum PGDOWN             enum END
enum INSRT
end-enum

\ The layout is according to the original IBM Personal Computer.

\ These tables translate scancodes to an internal representation for
\ keystrokes, which is a superset of ASCII.

: TBLSC-SPECIAL
    F1 c, F2 c, F3 c, F4 c, F5 c, F6 c, F7 c, F8 c, F9 c, F10 c,
    00 c, 00 c,   00 c,   UP c,
    00 c, 00 c, LEFT c,   00 c, RIGHT c,
    (  )  00 c,   00 c, DOWN c,
;

: | char c, ;

\ \ Like allot but initialize with zero the memory.
: zallot ( n -- )
    here >r dup allot r>
    swap 0 fill ;

: tblsize here swap - ;

: end. tblsize 256 swap - zallot ;


CREATE TBLSC
( )
( ) 0 c, ESC c,
( )       | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 0 | - | =     BACK c,
( ) TAB c,  | q | w | e | r | t | y | u | i | o | p | [ | ]    RET c,
( ) CTRL c,  | a | s | d | f | g | h | j | k | l | ; | ' | `
( ) SHIFT c,  | \ | z | x | c | v | b | n | m | , | . | /    SHIFT c,
( ) PRSCR c, ALT c,     ______SPACE______ c,       CAPSLOCK c,
( )
( ) TBLSC-SPECIAL
( )
TBLSC END.

CREATE TBLSC-SHIFT
( )
( ) 0 c, ESC c,
( )       | ! | @ | # | $ | % | ^ | & | * | ( | ) | _ | +     BACK c,
( ) TAB c,  | Q | W | E | R | T | Y | U | I | O | P | { | }    RET c,
( ) CTRL c,  | A | S | D | F | G | H | J | K | L | : | " | ~
( ) SHIFT c,  | | | Z | X | C | V | B | N | M | < | > | ?    SHIFT c,
( ) PRSCR c, ALT c,     ______SPACE______ c,      CAPSLOCK c,
( )
( ) TBLSC-SPECIAL
( )
TBLSC-SHIFT END.


variable alt-level
variable ctrl-level
variable shift-level

: shift? 0 shift-level @ < ;
: ctrl?  0 ctrl-level @ < ;
: alt?   0 alt-level @ < ;

: break? ( sc -- flag ) 7 bit? ;
: make?  ( sc -- flag ) break? not ;
: ->make $7f and ;

\ Retrieve a possibly prefixed scancode and yield two cells in the
\ stack. If the scancode is not prefixed, prefix is 0.
: pkey ( -- prefix sc )
    scancode
    dup $e0 = if
        scancode
    else
        0 swap
    endif ;

\ Decompound a scancode as a 'make' scancode more a flag which
\ indicates if it is a make or a break scancode.
: rkey ( -- prefix sc make? )
    pkey
    dup make?
    swap ->make swap ;

: sc->ekey ( sc -- key )
    shift? if
        tblsc-shift + c@
    else
        tblsc + c@
    endif ;

: bool->sign ( flag -- +1\-1 )
    if 1 else -1 endif ;

\ Update special keys according to SC and MAKE?. If SC is a special
\ key, it will be replaced by a null scancode.
: process-special-keys ( sc make? -- sc|0 make? )
    over sc->ekey case
        SHIFT of dup bool->sign shift-level +! nip 0 swap endof
        CTRL  of dup bool->sign  ctrl-level +! nip 0 swap endof
        ALT   of dup bool->sign   alt-level +! nip 0 swap endof
    endcase ;


$1 constant shift-mod
$2 constant ctrl-mod
$4 constant alt-mod

: modifier ( -- modifiers )
    0 ( no modifiers )
    shift? if shift-mod or then
    ctrl? if ctrl-mod or then
    alt? if alt-mod or then ;

: ekey*
    begin
        rkey rot drop \ Ignore prefix
        process-special-keys
        ( make? ) if
            sc->ekey ?dup if exit endif
        else
            drop
        endif
    again ;

: ekey ( -- key modifier )
    ekey* modifier ;

: key
    begin
        ekey*
    $80 over <= while
        drop
    repeat ;

\ keyboard.fs ends here
