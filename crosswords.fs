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

ASSEMBLER ALSO
CROSSFORTH DEFINITIONS

variable state

code dp
    %D push,
    ret
end-code

code dp!
    %TOS , %D mov
    unwind
    ret
end-code

code sp
    %S , %eax mov
    %eax push,
    ret
end-code

code sp!
    %TOS , %S mov
    ret
end-code


\ Stack manipulation

code dup ( x -- x x )
    %TOS , %eax mov
    %eax push,
    ret
end-code

code drop ( x -- )
    unwind
    ret
end-code

code over ( x y -- x y x )
    1 #S , %eax mov
    %eax push,
    ret
end-code


\ Number conversion

\ Read a binary integer from the counted input stream in the %EDX
\ register. If it is not a number, EDX=0. Otherwise, EDX=1 and the
\ numeric value is returned in %EAX. */

variable base

builtin %digit-char
    \ Non-hexadecimal digit chars
     %eax , #char 9 u<= if
         %eax , #char 0 u>= if
            #char 0 , %eax sub
            ret
        then
    then
    \ Hexadecimal digit chars
    %eax , #char F u<= if
        %eax , #char A u>= if
            #char A 10 - , %eax sub
            ret
        then
    then
    %eax , #char f <= if
        %eax , #char a >= if
            #char a 10 - , %eax sub
            ret
        then
    then
    \ Invalid chars
    # -1 , %eax mov
    ret
end-builtin

\ Receive a character in EAX and return the value or -1 if it is not a
\ valid digit character according to the value of BASE.
builtin digit-char
    %digit-char call
    base , %edx mov
    %eax , %edx u>= if
        # -1 , %eax mov
    then
    ret
end-builtin


\ Very basic output

builtin clear-screen
    # 80 25 * , %ecx mov
    begin %ecx 0<> while
        %ecx dec
        # 32 , $B8000 #PTR8 >%ecx 2* mov
    repeat
    ret
end-builtin

\ EAX = Counted string.
builtin print
    clear-screen call
    [%eax] PTR8 , %ecx movz
    begin %ecx 0<> while
        [%eax] PTR8 >%ecx , %dl mov
        %dl , $B7FFE #PTR8 >%ecx 2* mov
        %ecx dec
    repeat
    ret
end-builtin

label message
ascii" Loading..."

builtin main
    message addr , %eax mov
    print call
    ret
end-builtin


\ Local Variables:
\ forth-local-words: ((("code" "builtin") definition-starter (font-lock-keyword-face . 2))
\                     (("end-code" "end-builtin") definition-ender (font-lock-keyword-face . 2)))
\ End:

\ crosswords.fs ends here
