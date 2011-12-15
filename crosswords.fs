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

also assembler

' %esi   alias %S
' %edi   alias %D
' [%esi] alias TOS

4 constant cell

: begin there ;

: 0<>
    # 0 2swap cmp
    long 0 #PTR jz ;

: while
    last-cell ;

: repeat
    >r #PTR jmp r> there swap patch-jump ;

: push, ( x -- )
    # cell , %S sub
    ( ) , TOS mov ;

: variable
    code
    # there
    0 t,
    there cfa!
    push,
    ret
    end-code ;


variable base
variable state

code dp
    %D push,
    ret
end-code

code dp!
    TOS , %D mov
    # cell , %S add
    ret
end-code

code dup
    tos , %eax mov
    %eax push,
    ret
end-code

code drop
    # 4 , %S add
    ret
end-code

label string
ascii" Hola mundo!                                      "

code main
    string PTR8 , %ecx movz
    begin %ecx 0<> while
        string PTR8 >%ecx , %al mov
        %al , $B7FFE #PTR8 >%ecx 2* mov
        %ecx dec
    repeat
    ret
end-code


\ Local Variables:
\ forth-local-words: ((("code") definition-starter (font-lock-keyword-face . 2))
\                     (("end-code") definition-ender (font-lock-keyword-face . 2)))
\ End:

\ crosswords.fs ends here
