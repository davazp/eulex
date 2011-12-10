\ disassem.fs -- Pseudo-disassembler (debugging)

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

\ This file implements an ad-hoc disassembler for non-primitive Forth
\ words. It is a big CASE for every opcodes which we emitted in some
\ other point basically, but it was useful for debugging control-flow
\ words specially. Eventually, however, if the Forth implementation
\ was good and mature and it would be able to run a good disassembler,
\ turning this code obsolete.

create distable 256 cells allot

: unknown-opcode
    ." [unkown opcode "
    dup c@ print-number
    ."  '"
    dup c@ emit
    ." ']"
    1+ ;

\ Initialize the entries with the unknown-opcode controller
: init-distable
    256 0 ?do
        ['] unknown-opcode
        distable i cells + !
    loop
;

: defdisam [compile] :noname ;
: ;dis     [compile] ; swap cells distable + ! ; immediate

: disassemble-name ( addr -- )
    dup print-hex-number
    unfind
    dup 0= if
        2drop
    else
        ."  <" type ." >"
    then
;

: disassemble-rel-name ( addr -- )
    dup @ 4 + + disassemble-name ;

: disassemble-instruction ( addr -- next-addr )
    dup addr-column
    dup c@ cells distable + @ execute cr ;

: ret? $c3 = ;
: disassemble-memory
    cr
    begin dup disassemble-instruction swap c@ ret? until
    drop ;

: disassemble ' disassemble-memory ;

' disassemble alias see

INIT-DISTABLE

$0f defdisam ( 0f 85 )
   1+ dup c@ case
       $85 of ." JNZ " 1+ dup disassemble-rel-name cell + endof
       $84 of ." JZ "  1+ dup disassemble-rel-name cell + endof
       nip
   endcase
;dis

$29 defdisam ( 29 f8 )
." SUBL %EDI, %EAX" 2+ ;dis

$47 defdisam ." INCL (%EDI)" 1+ ;dis

$83 defdisam
1+ dup c@
   case
       $0c4 of
           ." ADDL $"
           dup 1+ c@ print-hex-number
           ." , %ESP"
           2 +
       endof
       $0c7 of ." ADDL $4, %EDI" 2 + endof
       $0c6 of ." ADDL $4, %ESI" 2 + endof
       $0ee of ." SUBL $4, %ESI" 2 + endof
       $0e8 of ." SUBL $4, %EAX" 2 + endof
       swap unknown-opcode swap
   endcase
;dis

$60 defdisam ." PUSHA" 1+ ;dis
$61 defdisam ." POPA"  1+ ;dis

$68 defdisam ." PUSH $" dup 1+ @ print-hex-number 5 + ;dis

$85 defdisam ( 85 c0 )
   ." TEST %EAX, %EAX"
   2+
;dis

$89 defdisam ( 89 07 )
   ." MOVL %EAX, (%EDI)" 2+
;dis

$8b defdisam ( 8b 06 )
   ." MOVL (%ESI), %EAX" 2+
;dis

$90 defdisam ." NOP" 1+ ;dis

$a1 defdisam ." MOVL " 1+ dup @ print-hex-number cell + ." , %EAX" ;dis

$b8 defdisam ." MOVL $" 1+ dup @ print-hex-number cell + ." , %EAX" ;dis

$e8 defdisam ." CALL " 1+ dup disassemble-rel-name cell + ;dis

$e9 defdisam ." JMP "  1+ dup disassemble-rel-name cell + ;dis

$eb defdisam ." JMP "  1+ dup dup c@ + 1+ print-hex-number 1+ ;dis

$c3 defdisam ." RET" 1+ ;dis

$c7 defdisam
   1+ dup c@
   case
       $46 of ." MOVL $" 2+ dup @ print-number ." , -4(%ESI)" cell +  endof
       $06 of ." MOVL $" 1+ dup @ print-number ." , (%ESI)"   cell +  endof
       $07 of ." MOVL $" 1+ dup @ print-number ." , (%EDI)"   cell +  endof
       swap unknown-opcode swap
   endcase
;dis

$cf defdisam ." IRET" 1+ ;dis

$fa defdisam ." CLI " 1+ ;dis

$fb defdisam ." STI " 1+ ;dis

$ff defdisam ( ff d0 ) 2+ ." CALL *%EAX" ;dis


\ Local Variables:
\ forth-local-words: ((("defdisam") definition-starter (font-lock-keyword-face . 2))
\                     ((";dis") definition-ender (font-lock-keyword-face . 2)))
\ End:

\ disassem.fs ends here
