\ exceptions.fs --

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

: ISR latestxt isr-register ;

: fatal-exception ( isrinfo-addr )
    ." Interrupt #"
    dup isrinfo-int-no ?
    ." with error code "
    dup isrinfo-err-code @ print-hex-number
    ." ." cr
    ." EAX = " dup isrinfo-eax @ print-hex-number cr
    ." ECX = " dup isrinfo-ecx @ print-hex-number cr
    ." EDX = " dup isrinfo-edx @ print-hex-number cr
    ." EBX = " dup isrinfo-ebx @ print-hex-number cr
    ." EBP = " dup isrinfo-ebp @ print-hex-number cr
    ." ESI = " dup isrinfo-esi @ print-hex-number cr
    ." EDI = " dup isrinfo-edi @ print-hex-number cr
    ." ESP = " dup isrinfo-useresp @ print-hex-number cr
    ." EIP = " dup isrinfo-eip @ print-hex-number cr
    ." CS  = " dup isrinfo-cs  @ print-hex-number cr
    ." EFLAGS = " dup isrinfo-eflags @ print-hex-number cr
    ." SS = "     dup isrinfo-ss     @ print-hex-number cr
    drop
    disable-interrupts
    backtrace
    halt ;

: division-by-zero-exception ( isrinfo-addr )
    isrinfo-eflags @ eflags!
    -10 throw
; 0 ISR

: debug-exception
    fatal-exception
; 1 ISR

: non-maskable-interrupt-exception
    fatal-exception
; 2 ISR

\ : breakpoint-exception
\     fatal-exception
\ ; 3 ISR

: overflow-exception
    fatal-exception
; 4 ISR

: out-of-bounds-exception
    fatal-exception
; 5 ISR

: invalid-opcode-exception
    fatal-exception
; 6 ISR

: no-coprocessor-exception
    fatal-exception
; 7 ISR

: double-fault-exception
    fatal-exception
; 8 ISR

: coprocessor-segment-overrun-exception
    fatal-exception
; 9 ISR

: tss-exception
    fatal-exception
; 10 ISR

: segment-not-present-exception
    fatal-exception
; 11 ISR

: stack-fault-excetion
    fatal-exception
; 12 ISR

: general-protection-fault-exception
    fatal-exception
; 13 ISR

: page-fault-exception
    fatal-exception
; 14 ISR

: unknown-interrupt-exception
    fatal-exception
; 15 ISR

: coprocessor-fault-exception
    fatal-exception
; 16 ISR

: alignment-check-exception ( 486+ )
    fatal-exception
; 17 ISR

: machine-check-exception ( Pentium/586+ )
    fatal-exception
; 18 ISR

: reserved-exception
    fatal-exception
;
19 ISR   20 ISR   21 ISR
22 ISR   23 ISR   24 ISR
25 ISR   26 ISR   27 ISR
28 ISR   29 ISR   30 ISR
31 ISR

\ exceptions.fs ends here
