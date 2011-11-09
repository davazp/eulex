\ Testing system

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

vocabulary test
only forth-impl also test definitions

variable check_failed
variable total_passed
variable total_failed

: line input_source_line @ ;
: column input_source_column @ ;

: print-line line print-number ;
: print-column column print-number ;

: first-test?
    total_passed @
    total_failed @ +
    0= ;

: check-sucessfully?
    first-test? if
        false
    else
        check_failed @ 0=
    then ;

: report-previous-check
    check-sucessfully? if ." done" then ;

: checking
    report-previous-check
    cr ." Checking "
    begin char dup 10 <> while
        emit
    repeat
    drop
;

: assert
    not if
        cr 5 spaces ." - unexpected result in " print-line ." :" print-column
        total_failed 1+!
        check_failed 1+!
    else
        total_passed 1+!
    then ;

: noassert
    not assert ;

: report
    report-previous-check cr cr
    ." SUMMARY: "
    total_passed @ print-number
    ." /"
    total_passed @
    total_failed @ +
    print-number
    ."  tests were passed." cr
;

: run-tests
    check_failed 0!
    total_passed 0!
    total_failed 0!
    also test
    @tests/tsuite.fs load-buffer
    report
    previous ;

only forth-impl definitions

\ tests.fs ends here
