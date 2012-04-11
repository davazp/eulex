\ corestage2.fs --- 

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

LIGHT GRAY UPON BLACK
.( Loading...) CR
:noname
    ." Loading " buffer>nt id. ." ..." cr
; load-buffer-print-hook !

require @memory.fs
require @tools.fs
require @kernel/interrupts.fs
require @kernel/exceptions.fs
require @kernel/irq.fs
require @kernel/timer.fs
require @kernel/floppy.fs
require @kernel/keyboard.fs
require @kernel/serial.fs
require @kernel/speaker.fs
require @tests/tests.fs
require @kernel/cpuid.fs
require @input.fs
require @debugger.fs

\ Rebooting the machine

: reboot
    beep
    disable-interrupts
    clear-kbd-buffer
    kdb-reset kbd-io outputb
    halt ;

\ Timing

variable execute-timing-start
: execute-timing ( xt -- ms )
    \ TODO: Replace . by u. when it exists.
    get-internal-run-time execute-timing-start !
    execute
    get-internal-run-time execute-timing-start @ -
    CR ." Execution took " . ." miliseconds of run time." ;

\ Date & Time
\ TODO: Move this to a better place

: cmos $70 outputb $71 ( 1 ms ) inputb ;
: cmos! $70 outputb $71 ( 1 ms ) outputb ;

variable bcd?
: ?bcd>bin bcd? @ if dup 4 rshift 10 * swap $f and + endif ;

: cmos-time-updating?
    $0b cmos $80 and ;
: wait-cmos-time-updating
    begin cmos-time-updating? not until ;

: decode-cmos-time  ( -- second minute hour date month year )
    wait-cmos-time-updating
    $0b cmos $04 and if bcd? off else bcd? on endif
    $00 cmos ?bcd>bin                   \ seconds
    $02 cmos ?bcd>bin                   \ minute
    $04 cmos ?bcd>bin                   \ hour
    $07 cmos ?bcd>bin                   \ date
    $08 cmos ?bcd>bin                   \ month
    $09 cmos ?bcd>bin                   \ year
;
: .date
    decode-cmos-time -rot swap
    print-number [char] / emit
    print-number [char] / emit
    print-number
    space
    print-number [char] : emit
    print-number [char] : emit
    print-number ;


\ Markers

: marker-restore-wordlist ( wid -- )
    begin here over wid>latest u<= while
        dup wid>latest previous-word over wid-latest !
    repeat
    drop ;

: marker-restore-wordlists
    last-wid @
    begin ?dup while
        dup marker-restore-wordlist
        wid-previous @
    repeat ;

: marker here create , does> @ dp! marker-restore-wordlists ;


( run-tests )

\ DEBUGGING. This is useful to run the QEMU on emacs, and use Eulex
\ like anyother Forth implementation!

\ : serial-loop
\     ." Initializing serial port interface..." cr
\     ['] read-byte input_routine ! ;

\ serial-echo-on
\ serial-loop

enable-interrupts
initialize-floppy

require @user.fs

\ corestage2.fs ends here
