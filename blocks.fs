\ blocks.fs --

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

\ TODO: Support more than a single buffer

require @kernel/floppy.fs
require @memory.fs
require @structures.fs

defer read-block-from-backend
defer write-block-to-backend

:noname -100 throw ; is read-block-from-backend
:noname -100 throw ; is write-block-to-backend

variable current-block
-1 current-block !

variable updated?
' floppy-buffer alias block-buffer

: update updated? on ;

: flush
    updated? @ if
        current-block @ write-block-to-backend
        updated? off
    endif ;

: block ( u -- addr )
    dup current-block @ = if drop else
        flush dup current-block !
        read-block-from-backend 
    endif
    block-buffer ;

: buffer ( u -- addr )
    dup current-block @ = if drop else
        flush current-block !
    endif
    block-buffer ;


variable scr

: .2 dup 10 < if space then . ;

: list ( u -- )
    dup scr ! block
    16 0 ?do cr i .2 dup 64 -trailing type 64 + loop
    drop ;

' flush alias save-buffers

\ Floppy backend

: read-block-from-floppy ( u -- )
    2* lba 2 read-sectors ;

: write-block-to-floppy ( u -- )
    2* lba 2 write-sectors ;

: use-floppy
    flush
    detect-drive not if -100 throw then
    ['] read-block-from-floppy is read-block-from-backend
    ['] write-block-to-floppy is write-block-to-backend ;


\ blocks.fs ends here
