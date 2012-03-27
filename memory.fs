\ memory.fs -- Heap allocation

\   This file provides support for Forth words ALLOCATE, FREE and
\   RESIZE by a simple first-fit strategy implementation.

\ Copyright 2011,2012 (C) David Vazquez

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

require @string.fs
require @structures.fs
require @kernel/multiboot.fs

\ Heap region memory limits. It covers from the end of the dictionary
\ to the end of the upper memory as provided by the
\ multiboot-compliant bootloader.
dp-limit 2aligned constant heap-start
mem-upper-limit 1- 2 cells - 2aligned constant heap-end

heap-start heap-end - constant heap-size

struct
    ( reserved ) cell noname field                       
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
    chunk-alloc% - ;

: chunk>end ( chunk -- )
    dup chunk>addr swap chunk>size + ;

\ Sentinel node. It is kept to make sure that there is always a first
\ node in the list, which makes easier the implementation.
heap-start        constant sentinel-chunk-begin
heap-end chunk% - constant sentinel-chunk-end

: align-chunk-size ( u -- u* )
    dup cell negate u<= if
        2aligned
    else
        drop $ffffffff
    then ;

: validate-chunk-size ( u -- u* )
    align-chunk-size dup chunk% u<= if
        drop chunk%
    endif ;

\ Note that all the following words work for available/free
\ chunks. So, when you read `chunk' in the code, you should think
\ about free chunk.  However, some operations can be used on chunks of
\ allocated memory.

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

: enough-large-chunk? ( u chunk -- flag )
    chunk>size u<= ;

: find-enough-chunk ( u -- chunk )
    first-chunk
    begin dup null-chunk? not while
        2dup enough-large-chunk? if nip exit endif
        next-chunk
    repeat
    nip ;

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

: adjust-chunk-size ( u chunk -- )
    chunk-size ! ;

: chunk-header ( start end -- chunk )
    over - chunk-alloc% - swap tuck adjust-chunk-size ;

: create-chunk ( start end -- )
    chunk-header
    dup find-preceding-chunk
    swap tuck insert-chunk ;

: expand-chunk ( u chunk -- )
    tuck chunk>size + swap adjust-chunk-size ;

: reduce-chunk ( u chunk -- )
    swap negate swap expand-chunk ;

: too-large-chunk? ( n chunk -- flag )
    chunk>size swap chunk% + 2* u>= ;

\ Resize CHUNK to U and return a new available new-chunk.
: split-allocated-chunk ( u chunk -- new-chunk )
    dup chunk>end >r
    tuck adjust-chunk-size
    chunk>end r> create-chunk ;

: reserve-chunk ( u chunk -- new-chunk )
    2dup too-large-chunk? if
        dup chunk>end >r
        tuck reduce-chunk
        chunk>end r> chunk-header
    else
        nip dup delete-chunk
    endif ;


\ Coalescing

: adjoint-chunks? ( chunk1 chunk2 -- flag )
    swap chunk>end = ;

: limit-chunks? ( chunk1 chunk2 -- flag )
    first-chunk? swap last-chunk? or ;

: coalescable? ( chunk1 chunk2 -- flag )
    2dup adjoint-chunks? -rot limit-chunks? not and ;

: absorb-chunk ( chunk1 chunk2 -- )
    chunk>size chunk-alloc% + swap expand-chunk ;

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

: allocate ( u -- a-addr error )
    validate-chunk-size
    dup find-enough-chunk
    dup null-chunk? if
        2drop 0 -1
    else
        reserve-chunk chunk>addr 0
    endif ;

: free ( a-addr -- error )
    addr>chunk dup chunk>end create-chunk try-coalesce drop 0 ;


: reallocate-memory ( addr1 addr2 u -- error )
    \ Copy u bytes from ADDR1 to ADDR2 and free ADDR1.
    rot dup >r -rot move r> free ;

: resize-with-reallocation ( addr u -- addr error )
    dup allocate ?dup if
        2>r 2drop 2r>
    else
        dup >r swap reallocate-memory r> swap
    endif ;

: resize-without-reallocation ( addr u -- addr error )
    swap addr>chunk
    2dup too-large-chunk? if
        tuck split-allocated-chunk try-coalesce drop
    else
        nip
    endif
    chunk>addr 0 ;

: resize ( a-addr u -- a-addr error )
    validate-chunk-size
    over addr>chunk chunk>size over u< if
        resize-with-reallocation
    else
        resize-without-reallocation
    endif ;


\ memory.fs ends here
