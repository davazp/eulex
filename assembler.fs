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

\ Cross-assembler:
\ Difference between the dictionary pointer to the target address.
0 value target-offset
: there here target-offset + ;

: lb dup 255 and c, ;
: 8>> 8 rshift ;

: byte lb drop ;
: word lb 8>> lb drop ;
: dword lb 8>> lb 8>> lb 8>> lb drop ;

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
\
\     base + index*scale + displacement
\
\ where BASE and INDEX are 32bits registers, SCALE is 1, 2 or 4, and
\ DISPLACEMENT is an immediate offset.
\
\ The following variables contain each one of the parts in the general
\ addressing mode. A value of -1 where a register is expected means
\ that it is omitted. Note that is it not the ModR/M either SIB
\ bytes. They are encoded later from this variables, however.
variable base
variable index
variable scale
variable displacement

: reset-addressing-mode
    -1 base !
    -1 index !
    1 scale !
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

: 1* 1 S ;
: 2* 2 S ;
: 4* 4 S ;
: 8* 8 S ;

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


\ INSTRUCTION ENCODING

\ Parts of the instruction and the size in bytes of them in the
\ current instruction. A size of zero means this part is not present.
variable inst-size-override?
variable inst-opcode            variable inst-opcode#
variable inst-modr/m            variable inst-modr/m#
variable inst-sib               variable inst-sib#
variable inst-disp              variable inst-disp#
variable inst-imm               variable inst-imm#

: 0! 0 swap ! ;
: 0F, $0F byte ;        ( extended opcode )
: 66, $66 byte ;

\ Initialize the assembler state for a new instruction. It must be
\ called in the beginning of each instruction.
: reset-instruction
    reset-addressing-mode
    inst-size-override? off
    inst-opcode 0!              1 inst-opcode# !
    inst-modr/m 0!              inst-modr/m# 0!
    inst-sib 0!                 inst-sib# 0!
    inst-disp 0!                inst-disp# 0!
    inst-imm 0!                 inst-imm# 0!
; latestxt execute

\ Words to fill instruction's data

\ Set the size-override prefix.
: size-override inst-size-override? on ;

\ Set some bits in the opcode field.
: |opcode ( u -- )
    inst-opcode @ or inst-opcode ! ;

: clear-bits ( mask value -- value* )
    swap invert and ;

: set-bits! ( x mask addr -- )
    dup >r @ over swap clear-bits -rot and or r> ! ;

: set-modr/m-bits!
    inst-modr/m set-bits!
    1 inst-modr/m# ! ;

: set-sib-bits!
    inst-sib set-bits!
    1 inst-sib# ! ;

: mod!    6 lshift %11000000 set-modr/m-bits! ;
: op/reg! 3 lshift %00111000 set-modr/m-bits! ;
: r/m!             %00000111 set-modr/m-bits! ;

: s! 6 lshift %11000000 set-sib-bits! ;
: i! 3 lshift %00111000 set-sib-bits! ;
: b!          %00000111 set-sib-bits! ;

\ Set the displacement field.
: disp! inst-disp ! ;           : disp#! inst-disp# ! ;
: disp8! disp! 1 disp#! ;
: disp32! disp! 4 disp#! ;

\ Set the immediate field.
: imm! inst-imm ! ;             : imm#! inst-imm# ! ;

: flush-value ( x size -- )
    case
        0 of drop  endof
        1 of byte  endof
        2 of word  endof
        4 of dword endof
        true abort" Invalid number of bytes."
    endcase ;

: flush
    \ Prefixes
    inst-size-override? @ if 66, endif
    \ Opcode, modr/m and sib
    inst-opcode @ inst-opcode# @ flush-value
    inst-modr/m @ inst-modr/m# @ flush-value
    inst-sib    @ inst-sib#    @ flush-value
    \ Displacement and immediate
    inst-disp @ inst-disp# @ flush-value
    inst-imm  @ inst-imm#  @ flush-value
    reset-instruction ;


\ MEMORY REFERENCE ENCODING

: <=x<= ( n1 n2 n3 -- n1<=n2<=n3 )
    over -rot <= >r <= r> and ;

: 8-bit? ( n -- flag )
    -128 swap 127 <=x<= ;

\ return the mod value for a given displacement.
: disp>mod ( n -- 0|1|2 )
    ?dup 0= if 0 else
        8-bit? if 1 else 2 then
    endif ;

: scale>s ( scale -- s )
    case
        1 of 0 endof
        2 of 1 endof
        4 of 2 endof
        8 of 3 endof
        true s" Bad scale value."
    endcase ;

: null-displacement? displacement @ 0= ;

\ Encode the displacement in the displacement field and the mod field
\ of the modr/m byte. It is a general encoding which may be necessary
\ to modify for special rules.
: encode-displacement
    displacement @ dup disp>mod dup mod!
    case
        0 of 0 disp#! drop    endof
        1 of 1 disp#! disp8!  endof
        2 of 4 disp#! disp32! endof
    endcase ;

\ Encode memory references where there is not an index register. It
\ covers memory references of the form BASE + DISP, where BASE and
\ DISP are optional.
: encode-non-indexed-mref
    scale @ 1 <> abort" Scaled memory reference without index."
    base @ -1 = if
        5 r/m! displacement @ disp32!   \ only displacement
    else
        encode-displacement
        \ Special case: the ModR/M byte cannot encode [%EBP] as it is
        \ used to encode `only displacement' memory references, so we
        \ force a 8bits zero displacement.
        %ebp nip base @ = null-displacement? and if 1 mod! 0 disp8! endif
        \ Encode the base register in the ModR/M byte. If it is %esp,
        \ it requires to include the SIB byte.
        base @ r/m!
        \ NOTE: 4 means no index in SIB.
        %esp nip base @ = if base @ B! 4 I! endif
    endif ;

\ Encode memory references with an index register. It is encoded to
\ the SIB byte generally.
: encode-indexed-mref
    base @ -1 = if
        \ Special case: INDEX*SCALE + DISP. If SCALE is 1, we can
        \ encode the memory reference as a non-indexed. Otherwise, we
        \ have to force disp to 32bits.
        scale @ 1 = if
            index @ base ! -1 index ! encode-non-indexed-mref
        else
            0 mod! 4 r/m!
            scale @ scale>s s! index @ I! 5 B!
            displacement @ disp32!
        endif
    else
        \ More general addressing mode. We write R/M to 4 to specify a
        \ SIB byte, and write scale, index and base to it.
        encode-displacement 4 r/m!
        scale @ scale>s s! index @ i! base @ b!
    endif ;

\ Encode a general memory reference from the variables BASE, INDEX,
\ SCALE and DISPLACEMENT to the current instruction.
: encode-mref
    index @ -1 = if
        encode-non-indexed-mref
    else
        encode-indexed-mref
    endif ;


\ INSTRUCTION-DEFINING WORDS

\ Operands Pattern-maching
variable inst#op

: operands inst#op ! ;
' operands alias operand

: 2ops? inst#op @ 2 = ;

: 1-op-match ( op mask -- op flag )
    2 pick and 0<> ;

: 2-op-match ( op1 op2 mask1 mask2 -- op1 op2 flag )
    3 pick and 0<> swap
    5 pick and 0<> and ;

: op-match ( ops .. masks ... -- ops .. flag )
    inst#op @ 1 = if 1-op-match else 2-op-match then ;

\ Patterns
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
\ Multi-patterns
-1 constant any
al ax or eax or        constant acc
reg8 reg16 or reg32 or constant reg
mem8 mem16 or mem32 or constant mem
reg8 mem8 or           constant r/m8
reg16 mem16 or         constant r/m16
reg32 mem32 or         constant r/m32
reg mem or             constant r/m
\ any? matches with any type if the current instruction has 2
\ operands. Otherwise it is ignored.
: any? 2ops? if any then ;

: (no-dispatch)
    true abort" The instruction does not support these operands." ;

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

\ Encode some pieces of the instruction automatically.

\ Set size-override prefix if some of the operands is a r/m16.
: size-override?
    begin-dispatch
    any? r/m16 dispatch: size-override ::
    r/m16 any? dispatch: size-override ::
    exit
    end-dispatch ;

\ Encode both memory references and immediate (if there) to the ModR/M
\ byte and the Immediate field, respectively.
: encode-memory
    begin-dispatch
    mem any? dispatch: encode-mref ::
    any? mem dispatch: encode-mref ::
    exit
    end-dispatch ;

: encode-immediate-size
    \ NOTE: This is done automatically only if the instruction has
    \ _TWO_ operands. In which case, the size will match the size of
    \ the target operand. Instructions with 1 operand have to handle
    \ the immediate by themselves.
    2ops? if
        begin-dispatch
        imm r/m8  dispatch: 1 imm#! ::
        imm r/m16 dispatch: 2 imm#! ::
        imm r/m32 dispatch: 4 imm#! ::
        exit
        end-dispatch
    endif ;

\ This word can be called in the beginning of an instruction to encode
\ so much as we can automatically.
: instruction
    size-override? encode-memory encode-immediate-size ;

\ Check that the size of both operands is the same or signal an error.
: same-size
    begin-dispatch
      imm   any dispatch: ::
     r/m8  r/m8 dispatch: ::
    r/m16 r/m16 dispatch: ::
    r/m32 r/m32 dispatch: ::
    true abort" The size of the operands must match."
    end-dispatch ;

: immediate-operand 1 operand
    begin-dispatch
    imm dispatch: ::
    end-dispatch ;

\ Define an instruction with no operands
: single-instruction ( opcode -- )
    create c, does> 0 operands @ |opcode flush ;

: >reg op/reg! drop ;
: >opcode |opcode drop ;
: >imm imm! drop ;
: >imm8  >imm 1 imm#! ;
: >imm16 >imm 2 imm#! ;
: >imm32 >imm 4 imm#! ;

: >r/m
    inst#op @ >r
    1 operand begin-dispatch
    reg dispatch: 3 mod! r/m! drop ::
    mem dispatch: 2drop ::
    end-dispatch
    r> operands ;

: size-bit
    begin-dispatch
    any? r/m8  dispatch: 0 ::
    any? r/m16 dispatch: 1 ::
    any? r/m32 dispatch: 1 ::
    end-dispatch ;

: direction-bit
    begin-dispatch
    reg r/m dispatch: 0 ::
    r/m reg dispatch: 1 ::
    end-dispatch ;

: sign-extend-bit
    begin-dispatch
    imm r/m8 dispatch: 0 ::
    imm r/m dispatch:
        2swap dup >r 2swap r>
        8-bit? if 1 else 0 then ::
    end-dispatch ;

\ Set opcode and size bit.
: opcode-w |opcode size-bit |opcode ;
: opcode-wxxx |opcode size-bit 3 lshift |opcode ;
: opcode-dw opcode-w direction-bit 2 * |opcode ;
: opcode-sw opcode-w sign-extend-bit if 2 |opcode 1 imm#! endif ;

\ Generic 2 operand instructions.
: inst-imm-r/m opcode-w >r/m >imm ;
: inst-reg-reg opcode-w >r/m >reg ;
: inst-reg-r/m opcode-dw
    begin-dispatch
    reg r/m dispatch: >r/m >reg ::
    r/m reg dispatch: >reg >r/m ::
    end-dispatch ;


\ -------------------------------------------------------------------------

: ascii"
    [char] " parse dup byte
    here swap move ;


\ Arithmetic

: inst-imm-acc
    opcode-w 4 |opcode 2drop >imm ;

: arith-imm-r/m ( opext -- )
    >r $80 opcode-sw >r/m >imm r> op/reg! ;

: inst-binary-arithm ( opcode op-extension -- )
    2>r
    2 operands same-size instruction
    begin-dispatch
    imm acc dispatch:
        \ Here, you can encode as imm-r/m or imm-acc. We choose the
        \ shorter according to the size of the immediate value.
        sign-extend-bit if
            2r> nip arith-imm-r/m
        else
            2r> drop inst-imm-acc
        then ::
    imm r/m dispatch: 2r> nip arith-imm-r/m ::
    reg reg dispatch: 2r> drop inst-reg-reg ::
    r/m r/m dispatch: 2r> drop inst-reg-r/m ::
    end-dispatch
    flush ;

: adc $10 %010 inst-binary-arithm ;
: add $00 %000 inst-binary-arithm ;
: and $20 %100 inst-binary-arithm ;
: cmp $38 %111 inst-binary-arithm ;
: or  $08 %001 inst-binary-arithm ;
: sbb $18 %011 inst-binary-arithm ;
: sub $28 %101 inst-binary-arithm ;
: xor $30 %110 inst-binary-arithm ;

: inst-unary-arithm ( ext )
    >r 1 operand instruction
    begin-dispatch
    r/m dispatch: $F6 opcode-w >r/m r> op/reg! ::
    end-dispatch
    flush ;

: div  %110 inst-unary-arithm ;
: idiv %111 inst-unary-arithm ;
: imul %101 inst-unary-arithm ;  \ Binary version is not supported.
: mul  %100 inst-unary-arithm ;
: neg  %011 inst-unary-arithm ;
: not  %010 inst-unary-arithm ;

: inc 1 operand instruction
    begin-dispatch
    reg8 mem or dispatch: $FE opcode-w >r/m ::
    reg dispatch: $40 |opcode >opcode ::
    end-dispatch
    flush ;

: dec 1 operand instruction
    begin-dispatch
    reg8 mem or dispatch: $FE opcode-w >r/m 1 op/reg! ::
    reg dispatch: $48 |opcode >opcode ::
    end-dispatch
    flush ;


\ MOVement instructions

( This variant encode the register in the opcode. Used by MOV)
: inst-imm-reg* opcode-wxxx >opcode >imm ;

: mov 2 operands same-size instruction
    begin-dispatch
    imm reg dispatch: $B0 inst-imm-reg* ::
    imm mem dispatch: $C6 inst-imm-r/m  ::
    reg reg dispatch: $88 inst-reg-reg  ::
    r/m r/m dispatch: $88 inst-reg-r/m  ::
    end-dispatch
    flush ;

: movs 2 operands encode-memory
    begin-dispatch
    r/m8  reg16 dispatch: 66, 0F, $BE |opcode >reg >r/m ::
    r/m8  reg32 dispatch:     0F, $BE |opcode >reg >r/m ::
    r/m16 reg32 dispatch:     0F, $BF |opcode >reg >r/m ::
    end-dispatch
    flush ;

: movz 2 operands encode-memory
    begin-dispatch
    r/m8  reg16 dispatch: 66, 0F, $B6 |opcode >reg >r/m ::
    r/m8  reg32 dispatch:     0F, $B6 |opcode >reg >r/m ::
    r/m16 reg32 dispatch:     0F, $B7 |opcode >reg >r/m ::
    end-dispatch
    flush ;


\ Branching

: short-jump? ( target -- flag )
    there 2 + - 8-bit? ;

: rel8  there 2 + - >imm8 ;
: rel32 there 5 + - >imm32 ;

\ Base implementation for conditional jumps.

: inst-short-jcc ( target tttn -- )
    $70 |opcode |opcode rel8 flush ;
: inst-long-jcc ( target tttn -- )
    0F, $80 |opcode |opcode rel32 flush ;

: inst-jcc ( tttn -- ) >r immediate-operand instruction r>
    over short-jump? if inst-short-jcc else inst-long-jcc endif ;

: jo  %0000 inst-jcc ;          : jno  %0001 inst-jcc ;
: jb  %0010 inst-jcc ;          : jnb  %0011 inst-jcc ;
' jb  alias jnae                ' jnb  alias jae
: je  %0100 inst-jcc ;          : jne  %0101 inst-jcc ;
' je  alias jz                  ' jne  alias jnz
: jbe %0110 inst-jcc ;          : jnbe %0111 inst-jcc ;
' jbe alias jna                 ' jnbe alias ja
: js  %1000 inst-jcc ;          : jns  %1001 inst-jcc ;
: jp  %1010 inst-jcc ;          : jnp  %1011 inst-jcc ;
' jp  alias jpe                 ' jnp  alias jpo
: jl  %1100 inst-jcc ;          : jnl  %1101 inst-jcc ;
' jl  alias jnge                ' jnl  alias jge
: jle %1110 inst-jcc ;          : jnle  %1111 inst-jcc ;
' jle alias jng                 ' jnle alias jg

\ Unconditional jump
: jmp 1 operand instruction
    begin-dispatch
    imm dispatch: $E9 |opcode
        dup short-jump? if rel8 2 |opcode else rel32 endif ::
    r/m dispatch: $FF |opcode 4 op/reg! >r/m ::
    end-dispatch
    flush ;

\ Other instructions

: call 1 operand instruction
    begin-dispatch
    imm dispatch: $E8 |opcode there 5 + - >imm32 ::
    r/m dispatch: $FF |opcode 2 op/reg! >r/m ::
    end-dispatch
    flush ;

$94 single-instruction cbw
$99 single-instruction cdq

$F4 single-instruction clc
$FC single-instruction cld
$FA single-instruction cli

: cpuid 0F, $A2 |opcode flush ;

$F4 single-instruction hlt
$CF single-instruction iret

$90 single-instruction nop

$61 single-instruction popa
$9D single-instruction popf
$60 single-instruction pusha
$9C single-instruction pushf

$C3 single-instruction ret

$FB single-instruction sti


SET-CURRENT
PREVIOUS


\ Local Variables:
\ forth-local-words: ((("begin-dispatch" "end-dispatch" "dispatch:" "::")
\                      compile-only (font-lock-keyword-face . 2))
\                     (("single-instruction") immediate (font-lock-keyword-face . 2)))
\ End:

\ assembler.fs ends here
