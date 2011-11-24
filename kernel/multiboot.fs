\ multiboot.fs --

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

\ Transcription of multiboot.h from FSF to Forth

$1BADB002 constant multiboot-header-magic
$2BADB002 constant multiboot-bootloader-magic

struct
    32bits field multiboot-header-magic
    32bits field multiboot-header-flags
    32bits field multiboot-header-checksum
    32bits field multiboot-header-header-addr
    32bits field multiboot-header-load-addr
    32bits field multiboot-header-load-end-addr
    32bits field multiboot-header-bss-end-addr
    32bits field multiboot-header-bss-entry-addr
end-struct multiboot-header%

struct
    32bits field aout-symbol-table-tabsize
    32bits field aout-symbol-table-strsize
    32bits field aout-symbol-table-addr
    32bits field aout-symbol-table-reserved
end-struct aout-symbol-table%

struct
    32bits field elf-section-header-table-num
    32bits field elf-section-header-table-size
    32bits field elf-section-header-table-addr
    32bits field elf-section-header-table-shndx
end-struct elf-section-header-table%

\ The Multiboot information
struct
    32bits field multiboot-info-flags
    32bits field multiboot-info-mem-lower
    32bits field multiboot-info-mem-upper
    32bits field multiboot-info-boot-device
    32bits field multiboot-info-cmdline
    32bits field multiboot-info-mods-count
    32bits field multiboot-info-mods-addr
    \ The union of the structures
    elf-section-header-table% aout-symbol-table% max field multiboot-info-u
    32bits field multiboot-info-mmap-length
    32bits field multiboot-info-mmap-addr
end-struct multiboot-info%

\ The module structure
struct
    32bits field module-mod-start
    32bits field module-mod-end
    32bits field module-mod-string
    32bits field module-mod-reserved
end-struct module%

\ The memory map. Be careful that the offset 0 is base_addr_low but no size.
struct
    32bits field memory-map-size
    32bits field memory-map-base-addr-low
    32bits field memory-map-base-addr-high
    32bits field memory-map-length-low
    32bits field memory-map-length-high
    32bits field memory-map-type
end-struct memory-map%


\ multiboot.fs ends here
