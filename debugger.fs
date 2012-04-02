\ debugger.fs --

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

require @structures.fs
require @kernel/interrupts.fs

variable last-breakpoint

struct
    cell field breakpoint-nt
    cell field breakpoint-addr
    cell field breakpoint-byte
    cell field breakpoint-previous
    cell field breakpoint-next
end-struct breakpoint%

: breakpoint-enable? ( breakpoint -- )
    breakpoint-addr @ c@ $cc = ;

: enable-breakpoint ( breakpoint -- )
    dup breakpoint-enable? if drop else
        dup breakpoint-addr @ c@ over breakpoint-byte !
        $cc swap breakpoint-addr @ c!
    endif ;

: disable-breakpoint ( breakpoint -- )
    dup breakpoint-enable? not if drop else
        dup breakpoint-byte @ swap breakpoint-addr @ c!
    endif ;

: find-breakpoint ( addr -- breakpoint% )
    last-breakpoint @
    begin dup while
        2dup breakpoint-addr @ = if nip exit endif
        breakpoint-next @
    repeat
    nip ;

: install-breakpoint ( nt addr -- )
    dup find-breakpoint if 2drop
    else
        breakpoint% allocate throw
        tuck breakpoint-addr !
        tuck breakpoint-nt !
        last-breakpoint @ over breakpoint-next !
        0 over breakpoint-previous !
        dup last-breakpoint !
        enable-breakpoint
    endif ;

: delete-breakpoint ( breakpoint% -- )
    dup breakpoint-next @ ?dup if
        over breakpoint-previous @ ?dup if breakpoint-next ! endif
    endif
    dup breakpoint-previous @ ?dup if
        over breakpoint-next @ ?dup if breakpoint-previous ! endif
    endif
    dup disable-breakpoint
    free throw ;

: uninstall-breakpoint ( addr )
    find-breakpoint ?dup if
        dup breakpoint-byte @ over breakpoint-addr @ c!
        delete-breakpoint
    endif ;

: breakpoints
    last-breakpoint @
    begin dup while
        dup breakpoint-nt @ id. breakpoint-previous @
    repeat
    drop ;

: breakpoint-exception ( isrinfo -- )
    dup isrinfo-eip @ 1- find-breakpoint
    dup disable-breakpoint
    dup CR ." TRACE: The word " breakpoint-nt @  id. ." was called."
    breakpoint-addr @ swap isrinfo-eip !
; 3 ISR

: trace nt' dup nt>xt install-breakpoint ;
: untrace ' delete-breakpoint ;

\ debugger.fs ends here
