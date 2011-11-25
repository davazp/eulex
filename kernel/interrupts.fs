\ interrupts.fs --

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

require @vocabulary.fs
require @structures.fs

' cli alias disable-interrupts
' sti alias enable-interrupts

\ INTERRUPT DESCRIPTOR TABLE (IDT)

\ An integer from 32 to 256 which specify how many entries will have
\ the interrupt descriptor table. Set this variable correctly to hold
\ exceptions and IRQs.
48 constant idt-n-entries

struct
    16bits field idt-entry-base-low
    16bits field idt-entry-sel
     8bits field idt-entry-zero
     8bits field idt-entry-flags
    16bits field idt-entry-base-high
end-struct idt-entry%

idt-entry% idt-n-entries * constant idt-size
gdt-cs-selector constant selector
create idt-table idt-size allot

: low-word $FFFF and ;
: high-word 16 rshift low-word ;

: fill-idt-with-zeros
    idt-table idt-size 0 fill ;

: flush-idt
    idt-table idt-size 1- lidt ;

: write-isr-to-idt ( num addr flags -- )
    >r >r
    idt-entry% * idt-table +
    r@ high-word over idt-entry-base-high w!
    r> low-word  over idt-entry-base-low  w!
    0            over idt-entry-zero      c!
    selector     over idt-entry-sel       w!
    r>           swap idt-entry-flags     c!
;

\ Interrupt service routines (ISR)
\
\ ISRs cannot be written in Forth easily because we cannot handle how
\ the state of the machine change. Some of the more primitive parts
\ like CREATE..DOES> would become inmutable. Indeed, we write this
\ code in native code directly, using some auxiliary words because we
\ are missing an assembler.

struct
    \ Pushed by PUSHA
    32bits field isrinfo-edi
    32bits field isrinfo-esi
    32bits field isrinfo-ebp
    32bits field isrinfo-esp
    32bits field isrinfo-ebx
    32bits field isrinfo-edx
    32bits field isrinfo-ecx
    32bits field isrinfo-eax
    \ Interrupt number and error code
    32bits field isrinfo-int-no
    32bits field isrinfo-err-code
    \ Pushed by the processor automatically.
    32bits field isrinfo-eip
    32bits field isrinfo-cs
    32bits field isrinfo-eflags
    32bits field isrinfo-useresp
    32bits field isrinfo-ss
end-struct isrinfo%

\ A table of high level interrupt service routines written in normal
\ Forth. The routine ISR-DISPATCHER will dispatch to the right ISR
\ according to this table.
create isr-table idt-n-entries cells allot

: int>handler-slot
    cells isr-table + ;
: get-isr ( int -- handler )
    int>handler-slot @ ;
: set-isr ( int handler -- )
    swap int>handler-slot ! ;
: isr-execute ( int -- ... )
    get-isr execute ;

: isr-dispatcher
    rsp cell +
    dup isrinfo-int-no @ isr-execute ;

( Hide the ISR words to the system. They should not be called
from other Forth words because they use a different call convention. )
WORDLIST >ORDER DEFINITIONS

: cli   $fa c, ;
: sti   $fb c, ;
: pusha $60 c, ;
: popa  $61 c, ;
: iret  $cf c, ;
: call  $e8 c, here 4 + - , ;
: jmp   $e9 c, here 4 + - , ;      \ jmp X
: push-rstack
    \ push $N
    $68 c, , ;
: unwind-rstack
    \ addl $N, %esp
    $83 c, $c4 c, c, ;

CREATE isr-stub
    pusha
    ' isr-dispatcher call
    popa
    8 unwind-rstack
    sti
    iret

: ISR-ERRCODE ( n -- n addr )
    here over
    cli
    push-rstack
    isr-stub jmp ;

: ISR-NOERRCODE ( n -- n addr )
    here over
    cli
    0 push-rstack
    push-rstack
    isr-stub jmp ;

: ;; $8e write-isr-to-idt ;


DISABLE-INTERRUPTS
FILL-IDT-WITH-ZEROS

00 ISR-NOERRCODE ;; \ Division By Zero Exception
01 ISR-NOERRCODE ;; \ Debug Exception
02 ISR-NOERRCODE ;; \ Non Maskable Interrupt Exception
03 ISR-NOERRCODE ;; \ Breakpoint Exception
04 ISR-NOERRCODE ;; \ Into Detected Overflow Exception
05 ISR-NOERRCODE ;; \ Out of Bounds Exception
06 ISR-NOERRCODE ;; \ Invalid Opcode Exception
07 ISR-NOERRCODE ;; \ No Coprocessor Exception
08 ISR-ERRCODE   ;; \ Double Fault Exception
09 ISR-NOERRCODE ;; \ Coprocessor Segment Overrun Exception
10 ISR-ERRCODE   ;; \ Bad TSS Exception
11 ISR-ERRCODE   ;; \ Segment Not Present Exception
12 ISR-ERRCODE   ;; \ Stack Fault Exception
13 ISR-ERRCODE   ;; \ General Protection Fault Exception
14 ISR-ERRCODE   ;; \ Page Fault Exception
15 ISR-NOERRCODE ;; \ Unknown Interrupt Exception
16 ISR-NOERRCODE ;; \ Coprocessor Fault Exception
17 ISR-NOERRCODE ;; \ Alignment Check Exception (486+)
18 ISR-NOERRCODE ;; \ Machine Check Exception (Pentium/586+)
19 ISR-NOERRCODE ;; \ Reserved
20 ISR-NOERRCODE ;; \ Reserved
21 ISR-NOERRCODE ;; \ Reserved
22 ISR-NOERRCODE ;; \ Reserved
23 ISR-NOERRCODE ;; \ Reserved
24 ISR-NOERRCODE ;; \ Reserved
25 ISR-NOERRCODE ;; \ Reserved
26 ISR-NOERRCODE ;; \ Reserved
27 ISR-NOERRCODE ;; \ Reserved
28 ISR-NOERRCODE ;; \ Reserved
29 ISR-NOERRCODE ;; \ Reserved
30 ISR-NOERRCODE ;; \ Reserved
31 ISR-NOERRCODE ;; \ Reserved

FLUSH-IDT
ALSO FORTH-IMPL DEFINITIONS

: isr-register ( n addr )
    2dup set-isr
    drop dup 31 > if
        \ Write low level ISR to the IDT
        ISR-NOERRCODE ;;
    else drop then
;

PREVIOUS
PREVIOUS

\ interrupts.fs ends here
