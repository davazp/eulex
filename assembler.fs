\ assembler.fs ---

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

vocabulary Assembler
get-current
also Assembler definitions

: ` postpone postpone ; immediate

\ Display the hexadecimal values temporarily
: emit-byte hex. ;
: emit-word hex. ;

\ Instructions with no operands
: single-instruction ( opcode -- )
    create c, does> c@ emit-byte ;

HEX
60 single-instruction pusha
61 single-instruction popa
90 single-instruction nop
C3 single-instruction ret
CF single-instruction iret
FA single-instruction cli
FB single-instruction sti
DECIMAL

1 constant OP-AL
2 constant OP-AX
4 constant OP-EAX
8 constant OP-REG8
16 constant OP-REG16
32 constant OP-REG32
64 constant OP-SREG
128 constant OP-IMM
256 constant OP-MEM

\ Registers

: reg8  create , does> @  OP-REG8 swap ;
: reg16 create , does> @ OP-REG16 swap ;
: reg32 create , does> @ OP-REG32 swap ;
: sreg  create , does> @  OP-SREG swap ;

: %al  OP-AL OP-REG8 or 0 ;
: %ax  OP-AX OP-REG16 or 0 ;
: %eax OP-EAX OP-REG32 or 0 ;

( 0 reg32 %eax   0 reg16 %ax     0 reg8 %al )   0 sreg %es
1 reg32 %ecx     1 reg16 %cx     1 reg8 %cl     1 sreg %cs
2 reg32 %edx     2 reg16 %dx     2 reg8 %dl     2 sreg %ss
3 reg32 %ebx     3 reg16 %bx     3 reg8 %bl     3 sreg %ds
4 reg32 %esp     4 reg16 %sp     4 reg8 %ah     4 sreg %fs
5 reg32 %ebp     5 reg16 %bp     5 reg8 %ch     5 sreg %gs
6 reg32 %esi     6 reg16 %si     6 reg8 %dh
7 reg32 %edi     7 reg16 %di     7 reg8 %bh

\ Immediate values
: # OP-IMM ;


\ Memory references

\ The more general memory reference mode is
\     base + index*scale + displacement
\ where BASE and INDEX are 32bits registers, SCALE is 1, 2 or 4, and
\ DISPLACEMENT is an immediate offset.
\
\ The following variables contain each one of the parts in the general
\ addressing mode. A value of -1 where a register is expected means
\ that it is omitted. Note that is it not the ModR/M either thea SIB
\ bytes. They are encoded later from this variables, however.
variable base
variable index
variable scale
variable displacement

: check-reg32
    over OP-REG32 and abort" Addressing mode must use 32bits registers." ;

: B check-reg32 nip base ! ;
: I check-reg32 nip index ! ;
: S scale ! ;
: D displacement ! ;

: #MEM OP-MEM 0 ;
: PTR D #MEM ;

: 1* 0 S ;
: 2* 1 S ;
: 4* 2 S ;
: 8* 3 S ;

\ BASE                      BASE + DISP                   INDEX
: [%eax] %eax B #MEM ;       : +[%eax] D [%eax] ;          : >%eax %eax I ;
: [%ecx] %ecx B #MEM ;       : +[%ecx] D [%ecx] ;          : >%ecx %ecx I ;
: [%edx] %edx B #MEM ;       : +[%edx] D [%edx] ;          : >%edx %edx I ;
: [%ebx] %ebx B #MEM ;       : +[%ebx] D [%ebx] ;          : >%ebx %ebx I ;
: [%esp] %esp B #MEM ;       : +[%esp] D [%esp] ;          : >%esp %esp I ;
: [%ebp] %ebp B #MEM ;       : +[%ebp] D [%ebp] ;          : >%ebp %ebp I ;
: [%esi] %esi B #MEM ;       : +[%esi] D [%esi] ;          : >%esi %esi I ;
: [%edi] %edi B #MEM ;       : +[%edi] D [%edi] ;          : >%edi %edi I ;

\ Instructions

variable inst#op
variable instsize

: operands inst#op ! ;
' operands alias operand

: 32bits 32 instsize ! ;
: 16bits 16 instsize ! ;
:  8bits  8 instsize ! ;

\ Operands pattern maching

: 1-op-match ( op mask -- op flag )
    2 pick and 0<> ;

: 2-op-match ( op1 op2 mask1 mask2 -- op1 op2 flag )
    3 pick and 0<> swap
    5 pick and 0<> and ;

: op-match ( ops .. masks ... -- ops .. flag )
    inst#op @ 1 = if 1-op-match else 2-op-match then ;

\ Patterns for the dispatcher
' OP-AL    alias al
' OP-AX    alias ax
' OP-EAX   alias eax
' OP-REG8  alias reg8
' OP-REG16 alias reg16
' OP-REG32 alias reg32
' OP-SREG  alias sreg
' OP-IMM   alias imm
' OP-MEM   alias mem
\ Multicase patterns
-1 constant any
al ax or eax or constant acc
reg8 reg16 or reg32 or constant reg
reg8  mem or constant r/m8
reg16 mem or constant r/m16
reg32 mem or constant r/m32

: (no-dispatch)
    true abort" The instruction does not support that operands." ;

0 constant begin-dispatch immediate

: dispatch:
    1+ >r
    ` op-match ` if
    r>
; immediate compile-only

: ::
    >r ` else r>
; immediate compile-only

: end-dispatch
    ` (no-dispatch)
    0 ?do ` then loop
; immediate compile-only


\ MOV

: mov-reg->reg
    2drop 2drop ;
: mov-mem->reg
    2drop 2drop ;
: mov-reg->mem
    2drop 2drop ;
: mov-imm->reg
    2drop 2drop ;
: mov-imm->mem
    2drop 2drop ;

: mov 2 operands
    begin-dispatch
    mem acc dispatch: ::
    acc mem dispatch: ::
    reg reg dispatch: mov-reg->reg ::
    mem reg dispatch: mov-mem->reg ::
    reg mem dispatch: mov-reg->mem ::
    imm reg dispatch: mov-imm->reg ::
    imm mem dispatch: mov-imm->mem ::
    end-dispatch ;


SET-CURRENT
( PREVIOUS )


\ Local Variables:
\ forth-local-words: ((("begin-dispatch" "end-dispatch" "dispatch:" "::")
\                      compile-only (font-lock-keyword-face . 2)))
\ End:

\ assembler.fs ends here
