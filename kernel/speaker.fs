\ speaker.fs --

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

: sound ( freq - )
    1193180 swap /
    $b6 $43 outputb
    dup low-byte  $42 outputb
        high-byte $42 outputb
    $61 inputb
    dup dup 3 or <> if
        3 or $61 outputb
    then ;

: nosound
    $61 inputb
    $fc and
    $61 outputb ;

: play ( freq ms )
    swap sound ms nosound ;

: beep 1000 10 play ;


vocabulary music
also music definitions

: note
    [compile] :
    [compile] literal
    300
    [compile] literal
    postpone play
    15
    [compile] literal
    postpone ms
    [compile] ;
;

16 note 0c      33 note 1c      65 note 2c      131 note 3c
17 note 0c#     35 note 1c#     69 note 2c#     139 note 3c#
18 note 0d      37 note 1d      73 note 2d      147 note 3d
19 note 0d#     39 note 1d#     78 note 2d#     155 note 3d#
21 note 0e      41 note 1e      82 note 2e      165 note 3e
22 note 0f      44 note 1f      87 note 2f      175 note 3f
23 note 0f#     46 note 1f#     92 note 2f#     185 note 3f#
24 note 0g      49 note 1g      98 note 2g      196 note 3g
26 note 0g#     52 note 1g#     104 note 2g#    208 note 3g#
27 note 0a      55 note 1a      110 note 2a     220 note 3a
29 note 0a#     58 note 1a#     116 note 2a#    233 note 3a#
31 note 0b      62 note 1b      123 note 2b     245 note 3b

262 note 4c     523 note 5c     1046 note 6c    2093 note 7c
277 note 4c#    554 note 5c#    1109 note 6c#   2217 note 7c#
294 note 4d     587 note 5d     1175 note 6d    2349 note 7d
311 note 4d#    622 note 5d#    1244 note 6d#   2489 note 7d#
330 note 4e     659 note 5e     1328 note 6e    2637 note 7e
349 note 4f     698 note 5f     1397 note 6f    2794 note 7f
370 note 4f#    740 note 5f#    1480 note 6f#   2960 note 7f#
392 note 4g     784 note 5g     1568 note 6g    3136 note 7g
415 note 4g#    831 note 5g#    1661 note 6g#   3322 note 7g#
440 note 4a     880 note 5a     1760 note 6a    3520 note 7a
466 note 4a#    932 note 5a#    1865 note 6a#   3729 note 7a#
494 note 4b     988 note 5b     1975 note 6b    3951 note 7b

' 4c   alias -c         ' 5c   alias c          ' 6c   alias +c
' 4c#  alias -c#        ' 5c#  alias c#         ' 6c#  alias +c#
' 4d   alias -d         ' 5d   alias d          ' 6d   alias +d
' 4d#  alias -d#        ' 5d#  alias d#         ' 6d#  alias +d#
' 4e   alias -e         ' 5e   alias e          ' 6e   alias +e
' 4f   alias -f         ' 5f   alias f          ' 6f   alias +f
' 4f#  alias -f#        ' 5f#  alias f#         ' 6f#  alias +f#
' 4g   alias -g         ' 5g   alias g          ' 6g   alias +g
' 4g#  alias -g#        ' 5g#  alias g#         ' 6g#  alias +g#
' 4a   alias -a         ' 5a   alias a          ' 6a   alias +a
' 4a#  alias -a#        ' 5a#  alias a#         ' 6a#  alias +a#
' 4b   alias -b         ' 5b   alias b          ' 6b   alias +b


only forth-impl definitions

: <music also music ; immediate
: music> previous   ; immediate

: birthday
    <music
    ." Hap" g  ." py "  g ." birth"  a ." day "  g ." to "   +c ." you" b           500 ms cr
    ." Hap" g  ." py "  g ." birth"  a ." day "  g ." to "   +d ." you" +c          500 ms cr
    ." Hap" g  ." py "  g ." birth" +g ." day " +e ." dear " +c ." u" b ." ser" a   500 ms cr
    ." Hap" +f ." py " +f ." birth" +e ." day " +c ." to "   +d ." you" +c          500 ms cr
    music>
;
