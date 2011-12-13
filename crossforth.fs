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

\ Debug symbols

: 32hex ( u -- )
    base @ hex swap s>d
    <# # # # # # # # # #>
    rot base ! ;

\ TODO: Port!
: latest-name
    latest name>string ( GNU/Forth ) ;

\ CROSSWORDS AND WORDLIST

variable latest-nt
0 latest-nt !

: latest latest-nt @ ;

: header-previous, latest t, ;
: header-name, parse-name dup tc, >t ;
: header-flags, 0 tc, ;
: header-cfa, 0 t, ;

: header
    header-previous,
    there
    header-name,
    header-flags,
    header-cfa,
    latest-nt ! ;

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
to-patch: entry-point

CR .( \ Crosscompiling...) CR

: code
    parse-name
    2dup nextname header
    ... 2dup type CR
    <asm
    nextname [A] label [F]
    there cfa! ;
: end-code [A] ret [F] asm> ;
: , ;

require crosswords.fs

... .( entry-point ) CR
THERE ENTRY-POINT
<ASM
debug call
cli
hlt
ASM>

s" eulex.core" DUMP BYE

\ crossforth.fs ends here
