\ cpuid.fs --

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

: toggle-bit-21 ( x -- y )
    [ 1 21 lshift ]L xor ;

eflags dup toggle-bit-21 eflags!
eflags xor 21 bit? feature __CPUID__

[IFDEF] __CPUID__
    : single-cpuid cpuid nip nip nip ;

    0 cpuid
    constant highest-basic-value
    create vendor-id-string , , ,

    : vendor-id
        vendor-id-string 12 ;

    vendor-id s" GenuineIntel" string= feature __INTEL__

    1 cpuid
    constant processor-signature
    constant processor-flag-ebx
    constant processor-flag-edx
    constant processor-flag-ecx

    processor-flag-ecx  0 bit? feature __SSE3__
    processor-flag-ecx  1 bit? feature __PCLMULDQ__
    processor-flag-ecx  2 bit? feature __DTES64__
    processor-flag-ecx  3 bit? feature __MONITOR__
    processor-flag-ecx  4 bit? feature __DS_CPL__
    processor-flag-ecx  5 bit? feature __VMX__
    processor-flag-ecx  6 bit? feature __SMX__
    processor-flag-ecx  7 bit? feature __EIST__
    processor-flag-ecx  8 bit? feature __TM2__
    processor-flag-ecx  9 bit? feature __SSSE3__
    processor-flag-ecx 10 bit? feature __CNXT_ID__
    \ ...reserved...
    processor-flag-ecx 12 bit? feature __FMA__
    processor-flag-ecx 13 bit? feature __CX16__
    processor-flag-ecx 14 bit? feature __XTPR__
    processor-flag-ecx 15 bit? feature __PDCM__
    \ ...reserved...
    processor-flag-ecx 17 bit? feature __PCID__
    processor-flag-ecx 18 bit? feature __DCA__
    processor-flag-ecx 19 bit? feature __SSE_4_1__
    processor-flag-ecx 20 bit? feature __SSE_4_2__
    processor-flag-ecx 21 bit? feature __X2APIC__
    processor-flag-ecx 22 bit? feature __MOVBE__
    processor-flag-ecx 23 bit? feature __POPCNT__
    processor-flag-ecx 24 bit? feature __TSC_DEADLINE__
    processor-flag-ecx 25 bit? feature __AES__
    processor-flag-ecx 26 bit? feature __XSAVE__
    processor-flag-ecx 27 bit? feature __OSXSAVE__
    processor-flag-ecx 28 bit? feature __AVX__
    \ ...reserved...
    \ processor-flag-ecx 31 bit? feature __NO_USED__

    processor-flag-edx 0 bit? feature __FPU__
    processor-flag-edx 1 bit? feature __VME__
    processor-flag-edx 2 bit? feature __DE__
    processor-flag-edx 3 bit? feature __PSE__
    processor-flag-edx 4 bit? feature __TSC__
    processor-flag-edx 5 bit? feature __MSR__
    processor-flag-edx 6 bit? feature __PAE__
    processor-flag-edx 7 bit? feature __MCE__
    processor-flag-edx 8 bit? feature __CX8__
    processor-flag-edx 9 bit? feature __APIC__
    \ ...reserved...
    processor-flag-edx 11 bit? feature __SEP__
    processor-flag-edx 12 bit? feature __MTRR__
    processor-flag-edx 13 bit? feature __PGE__
    processor-flag-edx 14 bit? feature __MCA__
    processor-flag-edx 15 bit? feature __CMOV__
    processor-flag-edx 16 bit? feature __PAT__
    processor-flag-edx 17 bit? feature __PSE_36__
    processor-flag-edx 18 bit? feature __PSN__
    processor-flag-edx 19 bit? feature __CLFSH__
    \ ...reserved...
    processor-flag-edx 21 bit? feature __DS__
    processor-flag-edx 22 bit? feature __ACPI__
    processor-flag-edx 23 bit? feature __MMX__
    processor-flag-edx 24 bit? feature __FXSR__
    processor-flag-edx 25 bit? feature __SSE__
    processor-flag-edx 26 bit? feature __SSE2__
    processor-flag-edx 27 bit? feature __SS__
    processor-flag-edx 28 bit? feature __HTT__
    processor-flag-edx 29 bit? feature __TM__
    \ ...reserved...
    processor-flag-edx 31 bit? feature __PBE__

[ENDIF]


: cpuflags
    [ifdef] __SSE3__            ." sse3 "       [then]
    [ifdef] __PCLMULDQ__        ." pclmuldq "   [then]
    [ifdef] __DTES64__          ." dtes64 "     [then]
    [ifdef] __MONITOR__         ." monitor "    [then]
    [ifdef] __DS_CPL__          ." ds_cpl "     [then]
    [ifdef] __VMX__             ." vmx "        [then]
    [ifdef] __SMX__             ." smx "        [then]
    [ifdef] __EIST__            ." eist "       [then]
    [ifdef] __TM2__             ." tm2 "        [then]
    [ifdef] __SSSE3__           ." ssse3 "      [then]
    [ifdef] __CNXT_ID__         ." cnxt-id "    [then]
    [ifdef] __FMA__             ." fma "        [then]
    [ifdef] __CX16__            ." cx16 "       [then]
    [ifdef] __XTPR__            ." xtpr "       [then]
    [ifdef] __PDCM__            ." pdcm "       [then]
    [ifdef] __PCID__            ." pcid "       [then]
    [ifdef] __DCA__             ." dca "        [then]
    [ifdef] __SSE_4_1__         ." sse_4_1 "    [then]
    [ifdef] __SSE_4_2__         ." sse_4_2 "    [then]
    [ifdef] __X2APIC__          ." x2apic "     [then]
    [ifdef] __MOVBE__           ." movbe "      [then]
    [ifdef] __POPCNT__          ." popcnt "     [then]
    [ifdef] __TSC_DEADLINE__  ." tsc_deadline " [then]
    [ifdef] __AES__             ." aes "        [then]
    [ifdef] __XSAVE__           ." xsave "      [then]
    [ifdef] __OSXSAVE__         ." osxsave "    [then]
    [ifdef] __AVX__             ." avx "        [then]
    [ifdef] __FPU__             ." fpu "        [then]
    [ifdef] __VME__             ." vme "        [then]
    [ifdef] __DE__              ." de "         [then]
    [ifdef] __PSE__             ." pse "        [then]
    [ifdef] __TSC__             ." tsc "        [then]
    [ifdef] __MSR__             ." msr "        [then]
    [ifdef] __PAE__             ." pae "        [then]
    [ifdef] __MCE__             ." mce "        [then]
    [ifdef] __CX8__             ." cx8 "        [then]
    [ifdef] __APIC__            ." apic "       [then]
    [ifdef] __SEP__             ." sep "        [then]
    [ifdef] __MTRR__            ." mtrr "       [then]
    [ifdef] __PGE__             ." pge "        [then]
    [ifdef] __MCA__             ." mca "        [then]
    [ifdef] __CMOV__            ." cmov "       [then]
    [ifdef] __PAT__             ." pat "        [then]
    [ifdef] __PSE_36__          ." pse-36 "     [then]
    [ifdef] __PSN__             ." psn "        [then]
    [ifdef] __CLFSH__           ." clfsh "      [then]
    [ifdef] __DS__              ." ds "         [then]
    [ifdef] __ACPI__            ." acpi "       [then]
    [ifdef] __MMX__             ." mmx "        [then]
    [ifdef] __FXSR__            ." fxsr "       [then]
    [ifdef] __SSE__             ." sse "        [then]
    [ifdef] __SSE2__            ." sse2 "       [then]
    [ifdef] __SS__              ." ss "         [then]
    [ifdef] __HTT__             ." htt "        [then]
    [ifdef] __TM__              ." tm "         [then]
    [ifdef] __PBE__             ." pbe "        [then]
;

: cpuinfo
    cr
    ." Vendor-ID: " vendor-id type cr
    ." Flags    : " cpuflags cr
;

\ cpuids.fs ends here
