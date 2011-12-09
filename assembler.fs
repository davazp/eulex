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

DECIMAL

\ Assembler output

0 value asmfd

\ Emit the low byte of a word without pop it
: lb dup 255 and asmfd emit-file throw asmfd flush-file throw ;
\ Shift 8 bits to the right
: 8>> 8 rshift ;

: byte lb drop ;                       (  8 bits )
: word lb 8>> lb drop ;                ( 16 bits )
: dword lb 8>> lb 8>> lb 8>> lb drop ; ( 32 bits )


\ Instructions with no operands
: single-instruction ( opcode -- )
    create c, does> c@ byte ;

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
256 constant OP-MEM8
512 constant OP-MEM16
1024 constant OP-MEM32

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

: reset-addressing-mode
    -1 base !
    -1 index !
    0 scale !
    0 displacement ! ;

: check-reg32
    over OP-REG32 and 0=
    abort" Addressing mode must use 32bits registers." ;

: B check-reg32 base ! DROP ;
: I check-reg32 index ! DROP ;
: S scale ! ;
: D displacement ! ;

\ For addressing modes without base
: #PTR8 D OP-MEM8 0 ;
: #PTR16 D OP-MEM16 0 ;
: #PTR32 D OP-MEM32 0 ;
' #PTR32 alias #PTR

: 1* 0 S ;
: 2* 1 S ;
: 4* 2 S ;
: 8* 3 S ;

\ BASE                               BASE + DISP                   INDEX
: [%eax] %eax B OP-MEM32 0 ;       : +[%eax] D [%eax] ;          : >%eax %eax I ;
: [%ecx] %ecx B OP-MEM32 0 ;       : +[%ecx] D [%ecx] ;          : >%ecx %ecx I ;
: [%edx] %edx B OP-MEM32 0 ;       : +[%edx] D [%edx] ;          : >%edx %edx I ;
: [%ebx] %ebx B OP-MEM32 0 ;       : +[%ebx] D [%ebx] ;          : >%ebx %ebx I ;
: [%esp] %esp B OP-MEM32 0 ;       : +[%esp] D [%esp] ;          ( %esp is not a valid index )
: [%ebp] %ebp B OP-MEM32 0 ;       : +[%ebp] D [%ebp] ;          : >%ebp %ebp I ;
: [%esi] %esi B OP-MEM32 0 ;       : +[%esi] D [%esi] ;          : >%esi %esi I ;
: [%edi] %edi B OP-MEM32 0 ;       : +[%edi] D [%edi] ;          : >%edi %edi I ;

\ Override size of the memory reference
: PTR8 NIP OP-MEM8 SWAP ;
: PTR16 NIP OP-MEM16 SWAP ;
: PTR32 NIP OP-MEM32 SWAP ; \ Default


\ PATTERN-MACHING

variable inst#op

: operands inst#op ! ;
' operands alias operand

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
' OP-MEM8  alias mem8
' OP-MEM16 alias mem16
' OP-MEM32 alias mem32
\ Multicase patterns
-1 constant any
al ax or eax or constant acc
reg8 reg16 or reg32 or constant reg
mem8 mem16 or mem32 or constant mem
reg8 mem8 or constant r/m8
reg16 mem16 or constant r/m16
reg32 mem32 or constant r/m32
reg mem or constant r/m

: (no-dispatch)
    true abort" The instruction does not support that operands." ;

0 constant begin-dispatch immediate

: ` postpone postpone ; immediate

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


\ INSTRUCTION ENCODING

\ Parts of the instruction and the size in bytes of them in the
\ current instruction. A size of zero means this part is not present.
variable inst-size-override?
variable inst-opcode
variable inst-opcode-size
variable inst-modr/m
variable inst-modr/m-size
variable inst-sib
variable inst-sib-size
variable inst-displacement
variable inst-displacement-size
variable inst-immediate
variable inst-immediate-size

\ Initialize the assembler state for a new instruction. It must be
\ called in the beginning of each instruction.

: 0! 0 swap ! ;
: instruction
    reset-addressing-mode
    inst-size-override? off
    inst-opcode 0!
    1 inst-opcode-size !
    inst-modr/m 0!
    inst-modr/m-size 0!
    inst-sib 0!
    inst-sib-size 0!
    inst-displacement 0!
    inst-displacement-size 0!
    inst-immediate 0!
    inst-immediate-size 0! ;

\ Words to fill instruction's data

\ Set the size-override prefix.
: size-override inst-size-override? on ;

\ Set some bits in the opcode field.
: |opcode ( u -- )
    inst-opcode @ or inst-opcode ! ;

\ Set some bits and mark as present the ModR/M byte.
: |modr/m ( u -- )
    1 inst-modr/m-size !
    inst-modr/m @ or inst-modr/m ! ;

\ Set some bits and mark as present the SIB byte.
: |sib ( u -- )
    1 inst-sib-size !
    inst-sib @ or inst-sib ! ;

variable disp/imm-size
: 32bits 4 disp/imm-size ! ;
: 16bits 2 disp/imm-size ! ;
:  8bits 1 disp/imm-size ! ;

\ Set the displacement field.
: displacement!
    disp/imm-size @ inst-displacement-size !
    inst-displacement ! ;

\ Set the immediate field.
: immediate!
    disp/imm-size @ inst-immediate-size !
    inst-immediate ! ;

: flush-value ( x size -- )
    case
        0 of drop  endof
        1 of byte  endof
        2 of word  endof
        4 of dword endof
        true abort" Invalid number of bytes."
    endcase ;

: flush-instruction
    \ Prefixes
    inst-size-override? @ if $66 byte endif
    \ Opcode, modr/m and sib
    inst-opcode @ inst-opcode-size @ flush-value
    inst-modr/m @ inst-modr/m-size @ flush-value
    inst-sib    @ inst-sib-size    @ flush-value
    \ Displacement and immediate
    inst-displacement @ inst-displacement-size @ flush-value
    inst-immediate    @ inst-immediate-size    @ flush-value ;



\ Set size-override prefix if some of the operands is a r/m16.
: size-override?
    begin-dispatch
    any r/m16 dispatch: size-override ::
    r/m16 any dispatch: size-override ::
    exit
    end-dispatch ;

: inst-imm-reg
    size-override?
    begin-dispatch
    imm reg8  dispatch: |opcode $0 |opcode DROP  8bits immediate! DROP ::
    imm reg16 dispatch: |opcode $8 |opcode DROP 16bits immediate! DROP ::
    imm reg32 dispatch: |opcode $8 |opcode DROP 32bits immediate! DROP ::
    end-dispatch ;



: <=x<= ( n1 n2 n3 -- n1<=n2<=n3 )
    over -rot <= >r <= r> and ;

\ Return the Mod value for a given displacement.
: disp>mod ( n -- 0|1|2 )
    dup 0= if 0 else
        -128 over 127 <=x<= if 1 else 2 then
    endif
    nip ;


\ Check that the size of both operands is the same or signal an error.
: same-size
    begin-dispatch
      imm   any dispatch: ::
     r/m8  r/m8 dispatch: ::
    r/m16 r/m16 dispatch: ::
    r/m32 r/m32 dispatch: ::
      any   any dispatch: true
    abort" The size of the operands must match." ::
    end-dispatch ;

: mov 2 operands same-size instruction
    s" forth.core" w/o bin create-file throw to asmfd
    begin-dispatch
    imm reg dispatch: $B0 |opcode inst-imm-reg ::
    imm r/m dispatch: ::
    mem acc dispatch: ::
    acc mem dispatch: ::
    r/m reg dispatch: ::
    reg r/m dispatch: ::
    end-dispatch
    flush-instruction
    asmfd close-file throw ;


SET-CURRENT
( PREVIOUS )


\ Local Variables:
\ forth-local-words: ((("begin-dispatch" "end-dispatch" "dispatch:" "::")
\                      compile-only (font-lock-keyword-face . 2)))
\ End:

\ assembler.fs ends here
