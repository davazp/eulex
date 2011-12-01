\ memory.fs -- Heap allocation

\   This file provides support for Forth words ALLOCATE, FREE and
\   RESIZE by a simple first-fit strategy implementation.

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

require @structures.fs
require @kernel/multiboot.fs

\ Heap region memory limits. It covers from the end of the dictionary
\ to the end of the upper memory as provided by the
\ multiboot-compliant bootloader.
dp-limit aligned  constant heap-start
mem-upper-limit   constant heap-end

heap-start heap-end - constant heap-size

struct
    cell field chunk-size
    0    field chunk>addr
end-struct chunk-alloc%

struct
    chunk-alloc% noname field
    cell field chunk-next           \ used if it is free.
    cell field chunk-previous       \ used if it is free.
end-struct chunk%

: chunk>size ( chunk -- u )
    chunk-size @ ;
: addr>chunk ( addr -- chunk )
    cell - ;

: chunk>end ( chunk -- )
    dup chunk>addr swap chunk>size + ;

\ Sentinel node. It is kept to make sure that there is always a first
\ node in the list, which makes easier the implementation.
heap-start        constant sentinel-chunk-begin
heap-end chunk% - constant sentinel-chunk-end


\ Note that all the following words work for available/free
\ chunks. So, when you read `chunk' in the code, you should think
\ about free chunk.

: next-chunk ( chunk -- next-chunk )
    chunk-next @ ;

: previous-chunk ( chunk -- previous-chunk )
    chunk-previous @ ;

: first-chunk ( -- chunk )
    sentinel-chunk-begin next-chunk ;

: first-chunk? ( chunk -- flag )
    first-chunk = ;

: last-chunk? ( chunk -- flag )
    next-chunk sentinel-chunk-end = ;

: null-chunk? ( chunk -- flag )
    sentinel-chunk-end = ;

: chunk-neighbours ( chunk -- previous next )
    dup previous-chunk swap next-chunk ;

: link-chunks ( chunk1 chunk2 -- )
    2dup swap chunk-next ! chunk-previous ! ;

: enough-large-chunk? ( n chunk -- flag )
    chunk>size u<= ;

: find-enough-chunk ( n -- chunk )
    first-chunk
    begin dup null-chunk? not while
        2dup enough-large-chunk? if nip exit endif
        next-chunk
    repeat
    nip ;

: between ( a b c -- a<=b<=c )
    over u>= >r u<= r> and ;

: preceding-chunk? ( addr chunk -- flag )
    dup -rot next-chunk between ;

: find-preceding-chunk ( addr -- chunk )
    sentinel-chunk-begin
    begin 2dup preceding-chunk? not while
        next-chunk
    repeat
    nip ;

: insert-chunk ( preceding chunk -- )
    2dup swap next-chunk link-chunks link-chunks ;

: delete-chunk ( chunk -- )
    chunk-neighbours link-chunks ;

: chunk-header ( start end -- chunk )
    over - cell - swap tuck chunk-size ! ;

: create-chunk ( start end -- )
    chunk-header
    dup find-preceding-chunk
    swap tuck insert-chunk ;

: adjust-chunk-size ( n chunk -- )
    chunk-size ! ;

: expand-chunk ( n chunk -- )
    tuck chunk>size + swap adjust-chunk-size ;

: too-large-chunk? ( n chunk -- flag )
    chunk>size 2 / u<= ;

: split-chunk ( n chunk -- )
    dup chunk>size >r
    tuck adjust-chunk-size
    chunk>end dup r> + create-chunk drop ;

: split-chunk-optionally ( n chunk -- )
    2dup too-large-chunk? if split-chunk else 2drop endif ;

: adjoint-chunks? ( chunk1 chunk2 -- flag )
    swap chunk>end = ;

: limit-chunks? ( chunk1 chunk2 -- flag )
    first-chunk? swap last-chunk? or ;

: coalescable? ( chunk1 chunk2 -- flag )
    2dup adjoint-chunks? -rot limit-chunks? not and ;

: absorb-chunk ( chunk1 chunk2 -- )
    chunk>size cell + swap expand-chunk ;

: try-coalesce-chunks ( chunk1 chunk2 -- chunk )
    2dup coalescable? if
        2dup absorb-chunk delete-chunk
    else
        drop
    endif ;

: try-coalesce ( chunk -- chunk )
    dup chunk-neighbours rot swap
    try-coalesce-chunks
    try-coalesce-chunks ;


( Initialization )
( )
( ) sentinel-chunk-begin chunk% 0 fill
( ) sentinel-chunk-end   chunk% 0 fill
( ) sentinel-chunk-begin sentinel-chunk-end link-chunks
( )
( ) sentinel-chunk-begin chunk% +
( ) sentinel-chunk-end
( ) create-chunk drop
( )
( ----------------- )

\ Public words

: allocate ( u -- a-addr error )
    dup find-enough-chunk
    dup null-chunk? if
        2drop 0 -1
    else
        tuck split-chunk-optionally
        dup delete-chunk
        chunk>addr 0
    endif ;

: free ( a-addr -- error )
    addr>chunk dup chunk>end create-chunk try-coalesce drop 0 ;

: resize ( a-addr u -- a-addr error )
;

\ memory.fs ends here
