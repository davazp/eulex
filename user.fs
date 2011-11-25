\ user.fs --

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

require @kernel/timer.fs
require @input.fs

page
." Welcome to Eulex!" cr
cr
." Copyright (C) 2011 David Vazquez" cr
." This is free software; see the source for copying conditions.  There is NO"  cr
." warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE." cr cr

: license
    ." This program is free software; you can redistribute it and/or modify" cr
    ." it under the terms of the GNU General Public License as published by" cr
    ." the Free Software Foundation; either version 3 of the License, or" cr
    ." (at your option) any later version." cr
    cr
    ." This program is distributed in the hope that it will be useful," cr
    ." but WITHOUT ANY WARRANTY; without even the implied warranty of" cr
    ." MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the" cr
    ." GNU General Public License for more details." cr
    cr
    ." You should have received a copy of the GNU General Public License" cr
    ." along with this program. If not, see http://www.gnu.org/licenses/." cr ;


: user-interaction
    query interpret ;

: start-user-interaction
    @eulexrc.fs require-buffer
    begin
        ['] user-interaction %catch-without-unwind
        ?dup 0<> if
            ." ERROR: "
            case
                 -1 of ." Aborted" cr endof
                 -3 of ." Stack overflow" cr endof
                 -4 of ." Stack underflow" cr endof
                -10 of ." Division by zero" cr endof
                -13 of ." Unknown word" cr endof
                -14 of ." Compile-only word" cr endof
                ." Ocurred an unexpected error of code " dup . cr
            endcase
            backtrace
            %unwind-after-catch
            state 0!
            clearstack
        then
    again ;

START-USER-INTERACTION

\ user.fs ends here
