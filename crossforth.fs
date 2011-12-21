\ crossforth.fs --

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

require assembler.fs

vocabulary crossforth
only forth
also crossforth definitions

\ Target dictionary
: kb 1024 * ;
: mb 1024 kb * ;

\ Address of the start of the dictionary.
$100000 constant tbase

\ Buffer in memory where to keep the image of the target
\ dictionary. Make sure than the size of the allocated memory is
\ enough to keep the metacompiled core.
create tdict 1 mb allot
\ Target address to the next available in the dictionary.
variable tdp
tbase tdp !

: there tdp @ ;
: tallot tdp +! ;

\ Convert a adress in the target machine to a address in the host one.
: taddr>addr ( taddr -- addr )
    tbase - tdict + ;

\ Push the number of allocated bytes in the target dictionary.
: tdict-size ( -- u )
    tdp @ taddr>addr tdict - ;

: t@  taddr>addr @ ;
: t!  taddr>addr ! ;
: tc@ taddr>addr c@ ;
: tc! taddr>addr c! ;

: tc, there 1 tallot tc! ;
: t, there cell tallot t! ;

\ Store a null dword and create a word to patch the value later.
: to-patch: create there taddr>addr , 0 t, does> @ ! ;

\ Allocate U bytes from addr to the target dictionary.
: >t ( addr u -- )
    there taddr>addr swap dup tallot move ;

\ Dump the target dictionary to a filename in the data stack.
: dump ( addr u -- )
    r/w bin create-file throw
    dup tdict tdict-size rot write-file throw
    close-file throw ;

\ Assembler hooks
:noname there taddr>addr ;
there there taddr>addr -

ALSO ASSEMBLER
TO TARGET-OFFSET
IS WHERE
' tc, IS CASM,
' t, IS ASM,
: <asm also assembler ;
: asm> previous ;
PREVIOUS

' <asm alias [A] immediate
' asm> alias [F] immediate

\ TODO: Port!
: latest-name
    latest name>string ( GNU/Forth ) ;

\ CROSSWORDS AND WORDLIST

variable latest-nt
0 latest-nt !

: latest latest-nt @ ;

: header-previous, latest t, ;
: header-name, dup tc, >t ;
: header-flags, 0 tc, ;
: header-cfa, 0 t, ;

: header ( addr u -- )
    header-previous,
    there >r
    header-name,
    header-flags,
    header-cfa,
    r> latest-nt ! ;

: previous-word
    cell - t@ ;
: nt>name ( nt -- addr u )
    dup tc@ swap 1+ swap ;
: nt>flags ( nt -- flags )
    dup tc@ + 1+ ;
: nt>cfa
    nt>flags 1+ ;
: nt>pfa
    nt>cfa cell + ;
: nt>xt
    nt>cfa t@ ;
: latestxt
    latest nt>xt ;
: immediate? ( word -- flag )
    nt>flags tc@ 1 and ;
: cfa! ( xt -- )
    latest nt>cfa t! ;

\ MULTIBOOT

$1BADB002 constant MULTIBOOT-HEADER-MAGIC
$00010003 constant MULTIBOOT-HEADER-FLAGS
MULTIBOOT-HEADER-MAGIC
MULTIBOOT-HEADER-FLAGS + NEGATE constant MULTIBOOT-HEADER-CHKSM

\ Print the target address of compilation.
: ... THERE HEX. [char] : emit space ;

\ Store a dword and print the value.
: dword. dup t, hex. CR ;

CR .( \ Multiboot header ) CR
THERE
... .( magic = ) MULTIBOOT-HEADER-MAGIC dword.
... .( flags = ) MULTIBOOT-HEADER-FLAGS dword.
... .( chksm = ) MULTIBOOT-HEADER-CHKSM dword.
... .( header_addr = ) dword.
... .( load_addr = ) tbase dword.
... .( load_end_addr = ) 0 dword.
... .( bss_end_addr = ) 0 dword.
to-patch: entry-point!

CR .( \ Crosscompiling...) CR

\ Name a location in the target machine
: label create there , does> @ [A] #PTR [F] ;
: addr >r drop [A] # [F] r> ;

: debug there ;
: end-debug there over - swap taddr>addr swap discode ;

\ External symbol file. Generate a file eulex.symbols with the
\ translation of wordnames to the address of the code. It can be
\ useful for debugging hopefully.

s" eulex.symbols" w/o create-file throw value debug-file
debug-file file-size throw
debug-file reposition-file throw

: hexify ( u -- addr u )
    base @ >r
    hex s>d <# # # # # # # # # #>
    r> base ! ;

: add-debug-symbol-value ( value -- )
    hexify debug-file write-file throw ;

: add-debug-symbol-name ( addr u -- )
    debug-file write-file throw ;

: add-debug-symbol ( addr u value -- )
    add-debug-symbol-value
    s"  " debug-file write-file throw
    add-debug-symbol-name
    10 debug-file emit-file throw
    debug-file flush-file throw ;


\ Crossdefinitions

\ We define two ways to define crosswords:
\
\ o The couple `builtin' `end-builtin' define a location in the target
\   compilation address, you can compile a call or jump with `LABEL jmp/call'.
\   Arguments will be required in registers and memory mostly. They are
\   internal routines to the compiler/interpreter.
\
\ o The words `code' and `end-code' work as the above but it include
\   the word in the target wordlist, so they can be called from the
\   target Forth once it is built. They must accept arguments from the
\   data stack.
\

: builtin-label
    CR ... 2dup type 3 spaces
    2dup nextname label
    there add-debug-symbol ;

: builtin
    parse-name
    builtin-label <asm debug ;

: end-builtin
    end-debug asm> ;

: code
    parse-name
    2dup header
    builtin-label <asm debug there cfa! ;

: end-code
    end-debug asm> ;

: , ;

[A]
' %esi    alias %S              \ stack pointer
' %edi    alias %D              \ dictionary pointer
\ Yield a reference to the element in the N position in the data
\ stack, (first position is zero).
: #S cells +[%esi] ;
\ Yield a reference to the element in the top of the data stack.
: %TOS 0 #S ;
\ Immediate character
: #char # char ;

\ Primitive control-flow

: compare& ( ) ( ) 2swap cmp 0 #PTR ;

\ KLUDGE: LAST-CELL and PATCH-JUMP are not public words of the
\ assembler, they should not be used here. Implement stack-based
\ forward references in the assembler to replace it.
ALSO ASSEMBLER-IMPL
: last-jump last-cell ;
: jump-here there swap patch-jump ;
PREVIOUS

: <  compare& jnl ;             : >=  compare& jnge ;
: >  compare& jng ;             : <=  compare& jnle ;
: u< compare& jnb ;             : u>= compare& jnae ;
: u> compare& jna ;             : u<= compare& jnbe ;
: =  compare& jne ;             : <>  compare& je   ;
: 0=  # 0 =  ;
: 0<> # 0 <> ;

: if last-jump ;
: else >r long 0 #PTR jmp r> jump-here last-jump ;
: then jump-here ;

: begin there ;
: while last-jump ;
: repeat >r #PTR jmp r> jump-here ;

: wind ( imm/reg -- )
    # cell , %S sub ;

: unwind ( imm/reg -- )
    # cell , %S add ;

: push, ( imm/reg -- )
    wind
    ( ) , %TOS mov ;

: variable
    code
    # there
    0 t,
    there cfa!
    push,
    ret
    end-code ;

[F]

require crosswords.fs

THERE ENTRY-POINT!
builtin entry-point
    # 10 , base mov
    main call
    cli
    ## hlt
    << jmp
end-builtin

s" eulex.core" DUMP BYE

\ crossforth.fs ends here
