\ strings.fs -- Tests

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

Checking equivalence of strings...
:noname
    s" aaa" s" aaa" string= assert
    s" zzz" s" zzz" string= assert
    s" AAA" s" aaa" string<> assert
    s" ZZZ" s" aaa" string<> assert
; execute

Checking string comparison...
:noname
    s" aaa" s" zzz" compare -1 = assert
    s" zzz" s" aaa" compare  1 = assert
    s" abcd"  s" abcde" compare -1 = assert
    s" abcde" s" abcd"  compare  1 = assert
; execute

Checking -trailing...
:noname
    s" aaa"  s" aaa   " -trailing string= assert
    s" aaa"  s" aaa"    -trailing string= assert
; execute

Checking /string...
:noname
    s" xyz" s" abcxyz"    3 /string string= assert
    s" abcxyz" s" abcxyz" 0 /string string= assert
; execute

Checking string-prefix?...
:noname
    s" abc"    s" abc" string-prefix? assert
    s" abcdef" s" abc" string-prefix? assert
    s" ab"     s" abc" string-prefix? not assert
    s" xyz"    s" abc" string-prefix? not assert
; execute


\ strings.fs ends here
