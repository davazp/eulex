\ crosswords.fs --  Built-in words

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

: tos [A] [%esi] [F] ;

code dup
    tos  , %eax mov
    # 4  , %esi sub
    %eax , tos  mov
end-code

code drop
    # 4 , %esi add
end-code

code debug
    # char X , $B8000 #PTR8 MOV
end-code

\ crosswords.fs ends here
