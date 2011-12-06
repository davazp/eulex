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

\ IA-32 Instruction format
\
\     +--------+--------+--------+-------+--------------+------------+
\     | Prefix | Opcode | ModR/M |  SIB  | Displacement |  Immediate |
\     +--------+--------+--------+-------+--------------+------------+
\                           /         \
\                          /           \
\     7    6 5          3 2    0    7      6 5     3 2     0
\     +-----+------------+-----+    +-------+-------+------+
\     | Mod | Reg/Opcode | R/M |    | Scale | Index | Base |
\     +-----+------------+-----+    +-------+-------+------+
\

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

\ Operand number

2 constant max-operands
variable current-operand

\ Get the current operand number.
: op# current-operand @ ;

\ Initialize the operand number to 1.
: reset-op 1 current-operand ! ;
latestxt execute

\ Increase the operand number by 1.
: op 1 current-operand +! ;

\ Size and type of operands. The arrays OPERAND-SIZE and OPERAND-TYPE
\ keep the size (in bits, 0=unknown size) and the type of each
\ operand (immediate, register or memory reference).
create operand-size max-operands cells allot
create operand-type max-operands cells allot

1 constant OPREG
2 constant OPIMM
3 constant OPMEM

: reset-operands
    operand-size max-operands cells 0 fill
    operand-type max-operands cells 0 fill ;
latestxt execute

: opsize-index ( u -- addr )
    1- cells operand-size + ;
: optype-index ( u -- addr )
    1- cells operand-type + ;

: opsize! ( size -- )
    op# opsize-index ! ;

: optype! ( type -- )
    op# optype-index ! ;

: opsize ( u -- size )
    opsize-index @ ;

: optype ( u -- type )
    optype-index @ ;

\ Mark the type of the current operand to memory, register or
\ immediate respectively.
: mem OPMEM optype! ;
: reg OPREG optype! ;
: imm OPIMM optype! ;

\ Mark the size of the current operand to N bits.
: bits ( n -- ) opsize! ;


\ General purpose registers

: reg8  create , does> @  8 bits reg ;
: reg16 create , does> @ 16 bits reg ;
: reg32 create , does> @ 32 bits reg ;

0 reg32 %eax     0 reg16 %ax     0 reg8 %al
1 reg32 %ecx     1 reg16 %cx     1 reg8 %cl
2 reg32 %edx     2 reg16 %dx     2 reg8 %dl
3 reg32 %ebx     3 reg16 %bx     3 reg8 %bl
4 reg32 %esp     4 reg16 %sp     4 reg8 %ah
5 reg32 %ebp     5 reg16 %bp     5 reg8 %ch
6 reg32 %esi     6 reg16 %si     6 reg8 %dh
7 reg32 %edi     7 reg16 %di     7 reg8 %bh

\ Immediate values
: # imm ;

\ Memory references

\ The more general memory reference mode is
\     base + index*scale + displacement
\ where BASE and INDEX are 32bits registers, SCALE is 1, 2 or 4, and
\ DISPLACEMENT is an immediate offset.

variable base
variable index
variable scale
variable displacement

: B base ! ;
: I index ! ;
: S scale ! ;
: D displacement ! ;


SET-CURRENT
( PREVIOUS )

\ assembler.fs ends here
