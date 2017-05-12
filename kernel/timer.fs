\ timer.fs --

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
require @kernel/irq.fs

\ Yes, I know this could be more accurate. But I don't need
\ currently. If you feel that it is important, write it, please!

1193182 constant clock-tick-rate
1000 constant hz
clock-tick-rate hz / constant latch

$40 constant pit-channel0
: set-channel0-reload ( n -- )
    dup low-byte pit-channel0 outputb
        high-byte pit-channel0 outputb ;

\ Setup the PIT frequency to 1000 Hz (roughly)
latch set-channel0-reload

struct
    cell field timer-routine
    cell field timer-reset
    cell field timer-countdown
end-struct timer%

create TIMER0 timer% zallot
create TIMER1 timer% zallot
create TIMER2 timer% zallot
create TIMER3 timer% zallot

: reset-timer ( timer -- )
    dup timer-reset @ swap timer-countdown ! ;

: set-timer ( miliseconds xt timer -- )
    tuck timer-routine ! timer-reset ! ;

\ Implementation

: process-timer-tick ( timer -- )
    dup timer-countdown @ 0= if drop exit then
    dup timer-countdown 1-!
    dup timer-countdown @ 0= if timer-routine @ execute else drop then ;


variable internal-run-time
variable countdown

: irq-timer ( isrinfo ) drop
    internal-run-time 1+!
    countdown @ dup if 1- countdown ! else drop then
    TIMER0 process-timer-tick
    TIMER1 process-timer-tick
    TIMER2 process-timer-tick
    TIMER3 process-timer-tick
; 0 IRQ

: set-countdown countdown ! ;
: wait-for-countdown begin countdown @ while halt repeat ;

: get-internal-run-time internal-run-time @ ;

: time get-internal-run-time ;

\ Wait for (rougly) N milliseconds.
: ms ( n -- )
    set-countdown wait-for-countdown ;

\ timer.fs ends here
