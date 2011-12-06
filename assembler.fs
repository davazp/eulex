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


\ Size of operands. The array OPERAND-SIZE keeps the size in bits of
\ each operand (immediate, register or memory reference).
create operand-size max-operands cells allot
: reset-opsize
    operand-size max-operands cells 0 fill ;
latestxt execute
: opsize ( u -- size )
    1- cells operand-size + @ ;
: opsize! ( size -- )
    op# 1- cells operand-size + ! ;


SET-CURRENT
( PREVIOUS )

\ assembler.fs ends here
