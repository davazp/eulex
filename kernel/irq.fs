\ irq.fs --

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

require @kernel/interrupts.fs

\ PROGRAMMABLE INTERRUPT CONTROLLER (PIC)
$20 constant picm-command
$21 constant picm-data
$A0 constant pics-command
$A1 constant pics-data

: send-picm picm-command outputb io-wait ;
: send-pics pics-command outputb io-wait ;
: send-picm-data picm-data outputb io-wait ;
: send-pics-data pics-data outputb io-wait ;
: read-picm-data picm-data inputb ;
: read-pics-data pics-data inputb ;

: send-eoi ( irq -- )
    8 >= if $20 send-pics then
    $20 send-picm ;


\ Master PIC IRQs
$20
enum irq0       \ System timer
enum irq1       \ keyboard controller
enum irq2       \ IRQ9
enum irq3       \ Serial port controller for COM2 (shared with COM4, if present)
enum irq4       \ Serial port controller for COM1 (shared with COM3, if present)
enum irq5       \ LPT port 2 or sound card
enum irq6       \ Floppy disk controller
enum irq7       \ LPT port 1
\ Slave PIC IRQs
enum irq8       \ RTC Timer
enum irq9       \ The Interrupt is left open for the use of peripherals
enum irq10      \ The Interrupt is left open for the use of peripherals
enum irq11      \ The Interrupt is left open for the use of peripherals
enum irq12      \ Mouse on PS/2 connector
enum irq13      \ Math co-processor or integrated floating point unit
enum irq14      \ Primary ATA channel
enum irq15      \ Secondary ATA channel
end-enum

$01 constant ICW1-ICW4
$10 constant ICW1-INIT
$01 constant ICW4-8086

: save-irq-masks
    read-picm-data
    read-pics-data ;

: restore-irq-masks
    send-pics-data
    send-picm-data ;

: remap-master-irq ( offset -- )
    >r
    save-irq-masks
    ICW1-INIT ICW1-ICW4 + send-picm
    r> send-picm-data
    4  send-picm-data
    ICW4-8086 send-picm-data
    restore-irq-masks ;

: remap-slave-irq ( offset -- )
    >r
    save-irq-masks
    ICW1-INIT ICW1-ICW4 + send-pics
    r> send-pics-data
    2 send-pics-data
    ICW4-8086 send-pics-data
    restore-irq-masks ;

\ Remap IRQs
IRQ0 REMAP-MASTER-IRQ
IRQ8 REMAP-SLAVE-IRQ

: CREATE-IRQ ( xt n -- )
    noname create swap , , does>
    dup    @ execute
    cell + @ send-eoi ;

: IRQ ( n -- )
    latestxt over CREATE-IRQ
    irq0 + latestxt isr-register ;

: unhandled-irq
    isrinfo-int-no @ irq0 - send-eoi
;  0 IRQ   1 IRQ   2 IRQ   3 IRQ
   4 IRQ   5 IRQ   6 IRQ   7 IRQ
   8 IRQ   9 IRQ  10 IRQ  11 IRQ
  12 IRQ  13 IRQ  14 IRQ  15 IRQ

\ irq.fs ends here
