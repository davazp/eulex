\ serial.fs -- Serial port communication

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

vocabulary serial
also serial definitions

variable serial-echo

: serial-echo-on   true serial-echo ! ;
: serial-echo-off false serial-echo ! ;

serial-echo-off

$3f8 constant com1

$00 com1 1 + outputb \ Disable all interrupts
$80 com1 3 + outputb \ Enable DLAB (set baud rate divisor)
$03 com1 0 + outputb \ Set divisor to 3 (lo byte) 38400 baud
$00 com1 1 + outputb \ (hi byte)
$03 com1 3 + outputb \ 8 bits, no parity, one stop bit
$c7 com1 2 + outputb \ Enable FIFO, clear them, with 14-byte threshold
$0b com1 4 + outputb \ IRQs enabled, RTS/DSR set

: empty? com1 5 + inputb 32 and ;

: write-byte ( x -- )
    begin empty? until
    com1 outputb
;

: received?
    com1 5 + inputb 1 and ;

: read-byte ( -- x )
    begin received? until
    com1 inputb
    dup emit
    serial-echo @ if
        dup 13 = if
            10 write-byte
        else
            dup write-byte
        then
    then
;

previous definitions

\ serial.fs ends here
