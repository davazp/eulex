\ Video subsystem

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

\ Video

$b8000 constant video-addr

80 constant video-width
25 constant video-height

: video-memsize
    video-width video-height * 2* ;

: offset>cords ( offset -- row col )
    video-width /mod swap ;

: cords>offset ( row col -- offset )
    swap video-width * + ;

: v-offset ( row col -- offset )
    cords>offset 2* video-addr + ;

: v-glyph@ ( row col -- ch )
      v-offset c@ ;

: v-glyph! ( ch row col -- )
    v-offset c! ;

: v-attr@ ( row col -- attr )
    v-offset 1+ c@ ;

: v-attr! ( attr row col -- )
    v-offset 1+ c! ;

\ CRTC

$3D4 constant crtc-index
$3D5 constant crtc-data

: crtc! ( value reg -- )
    crtc-index outputb
    crtc-data  outputb ;


\ Cursor hardware

$0E constant crtc-index-location-high
$0F constant crtc-index-location-low

: low-byte $ff and ;

: high-and-low-bytes ( x -- high low )
    dup 8 rshift low-byte
    swap low-byte ;

: v-cursor-set-offset ( offset -- )
    high-and-low-bytes
    crtc-index-location-low  crtc!
    crtc-index-location-high crtc! ;

: v-cursor-set-position ( row col -- )
    cords>offset v-cursor-set-offset ;


\ video.fs ends here
