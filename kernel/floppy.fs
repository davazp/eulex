\ floppy.fs --

\ Copyright 2012 (C) David Vazquez

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

\ TODO: It is missing error checking and retries, so it will not work
\ on real hardware however I have not a real machine with floppy drive
\ to test it properly.

\ Registers
: MSR  $3F4 inputb ;
: DOR  $3F2 inputb ;    : DOR!  $3F2 outputb ;
: FIFO $3F5 inputb ;    : FIFO! $3F5 outputb ; 
: CCR! $3F7 outputb ;

\ ready to read/write?
: RQM MSR $80 and ;                             

\ Motors
: turn-on DOR $10 or DOR! ;
: turn-off DOR [ $10 invert ]L and DOR! ;

\ Commands
variable irq6-received

: detect-drive
    $10 $70 outputb $71 inputb 4 rshift
    4 = if exit else -100 throw then ;

: wait-until-ready
    128 0 ?do RQM if unloop exit endif 10 ms loop
    -100 throw ;

: read-data wait-until-ready FIFO ;
: write-data wait-until-ready FIFO! ;
: write-command irq6-received off write-data ;

: wait-for-irq begin irq6-received @ until ;

: specify ( -- )
    $03 write-command $df write-data $02 write-data ;

: version ( -- x )
    $10 write-command read-data dup . ;

: sense-interrupt ( -- st0 cyl )
    $08 write-command read-data read-data ;

: seek ( head cylinder -- )
    $0f write-command swap 2 lshift write-data write-data
    wait-for-irq sense-interrupt 2drop ;

: recalibrate
    $07 write-command $00 write-data
    wait-for-irq sense-interrupt 2drop ;

: transfer-ask ( s h c read? -- )
    if $c6 else $c5 then write-command    
    over 2 lshift write-data
    write-data write-data write-data
    2 write-data
    18 write-data
    $1b write-data
    $ff write-data ;

: transfer-vry ( -- st0 st1 st2 c h s )
    read-data read-data read-data
    read-data read-data read-data
    read-data ( 2 ) drop ;
    
: transfer ( c h s read? -- st0 st1 st2 c h s )
    >r -rot swap r>
    transfer-ask wait-for-irq transfer-vry ;

: read true transfer ;
: write false transfer ;


\ DMA Stuff

1024 constant dma-buffer-size

\ Align the dictionary to get a good buffer to do ISA DMA. The
\ conditions are: below 64MB and not to cross a 64KB boundary.
align
dp dma-buffer-size + dp $ffff or > [if]
    dp $ffff + $ffff0000 and dp!
[endif]
$01000000 here u<= [if]
    attr light red ." FATAL: ISA DMA Buffer is not below 64MB." cr attr!
    halt
[endif]

here dma-buffer-size allot constant dma-buffer

: flip-flop $ff $0c outputb ;

: dma-init
    disable-interrupts
    $06 $0a outputb                     \ mask channel 2
    \ Send buffer address
    flip-flop
    dma-buffer ( 0 rshift ) $04 outputb
    dma-buffer   8 rshift   $04 outputb
    dma-buffer  16 rshift   $81 outputb
    \ Send buffer size
    flip-flop
    dma-buffer-size 1- ( 0 rshift ) $05 outputb
    dma-buffer-size 1-   8 rshift   $05 outputb ;
: dma-read
    $46 $0b outputb
    $02 $0a outputb
    enable-interrupts ;                   \ unmask
: dma-write
    $4a $0b outputb
    $02 $0a outputb
    enable-interrupts ;                   \ unmask 

: disable-fdc $00 DOR! ;
: setup-fdc $00 CCR! ;
: enable-fdc $0C DOR! ;

: irq-floppy irq6-received on ; 6 IRQ

: initialize-floppy
    detect-drive
    irq6-received off
    disable-fdc
    enable-fdc
    wait-for-irq
    sense-interrupt 2drop
    setup-fdc
    specify
    turn-on 200 ms recalibrate turn-off ;

\ floppy.fs ends here
