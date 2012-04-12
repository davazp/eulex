\ exceptions.fs --

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

\ NOTE ON THE IMPLEMENTATION:
\    Exception handling relies on the couple of words CATCH...THROW.
\    CATCH installs an exception handler and THROW signals an exception,
\    jumping to the innermost exception handler.  The stack is unwinded
\    in CATCH time. Hence, we can access to the context of signal and
\    display useful debugging information (e.g: backtraces).

variable exception-handler

: exception-handler-target
    exception-handler @ 2 cells + @ ;
: exception-handler-previous
    exception-handler @ 1 cells + @ ;
: exception-handler-sp
    exception-handler @ 0 cells + @ ;
: drop-exception-handler
    exception-handler-previous exception-handler ! ;

: %throw ( n -- )
    dup if >r exception-handler-sp sp! r> then
    exception-handler-target jump ;

: %catch-without-unwind ( xt -- )
    \ Install exception handler
    exception-handler @ >r
    sp cell + >r
    rsp exception-handler !
    \ Execute XT
    execute
    0 %throw ;

: %unwind-after-catch
    r>
    exception-handler @ rsp!
    drop-exception-handler
    r> drop
    r> drop
    r> drop
    >r ;

: throw ( n -- )
    ?dup 0<> if %throw then ;

: catch ( ... XT -- ... n )
    %catch-without-unwind
    %unwind-after-catch ;

: abort -1 throw ;

\ exceptions.fs ends here
