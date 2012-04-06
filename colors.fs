\ colors.fs --- 

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

\ Examples of usage:

: fg attr $0f and ;
: bg attr $70 and ;

: fg! attr [ $0f invert ]L and or attr! ;
: bg! 4 lshift attr [ $70 invert ]L and or attr! ;

: blinking attr $80 or attr! ;

variable masks
variable background?

: color create , does> @ masks @ or
    background? @ if bg! else fg! endif
    background? off
    0 masks ! ;

: upon background? on ;

0 color black
1 color blue
2 color green
3 color cyan
4 color red
5 color purple
6 color brown
7 color gray

: light 8 masks ! ;
: dark 0 masks ! ;

: yellow light brown ;
: white light gray ;
: magenta light purple ;

\ colors.fs ends here
