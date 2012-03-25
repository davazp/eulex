/* forth.S ---                                              -*- asm -*-

            Minimal on-the-metal Forth implementation for x86
*/

/* Copyright (C) 2011 David Vázquez Púa */
/*
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 */

#define GDT_CS_SELECTOR 0x08
#define GDT_CS_BASE 0
#define GDT_CS_LIMIT 0xffff
#define GDT_CS_TYPE 0x9A
#define GDT_DS_DESCRIPTOR 0x10
#define GDT_DS_BASE 0
#define GDT_DS_LIMIT 0xffff
#define GDT_DS_TYPE 0x92

.data
gdtr:
        .short gdt_table_end - gdt_table - 1
        .long gdt_table
gdt_table:
        /* NULL: {.base=0, .limit=0, .type=0}; */
        .long 0
        .long 0
	/* CODE: {.base=0, .limit=0xffff, .type=0x9A}; */
        .short GDT_CS_LIMIT
        .short GDT_CS_BASE & 0xFFFF
        .byte (GDT_CS_BASE >> 16) & 0xFF
        .byte GDT_CS_TYPE
        .byte 0xCF
        .byte (GDT_CS_BASE >> 24) & 0xFF
        /* DATA: {.base=0, .limit=0xffff, .type=0x92}; */
        .short GDT_DS_LIMIT
        .short GDT_DS_BASE & 0xFFFF
        .byte (GDT_DS_BASE >> 16) & 0xFF
        .byte GDT_DS_TYPE
        .byte 0xCF
        .byte (GDT_DS_BASE >> 24) & 0xFF
gdt_table_end:

/* Reload register segments, so the system will reload the cached
   segments from the GDT table. */
.text
flush_gdt:
        lgdt gdtr
        movw $0x10, %ax
        movw %ax, %ds
        movw %ax, %es
        movw %ax, %fs
        movw %ax, %gs
        movw %ax, %ss
        ljmp $8, $1f
1:      ret


/* Registers with a meaning (almost) persistent across the code:
   %esi --- Data stack.
   %edi --- First free data in the dictionary.
*/

/* Reserved space for data and control stacks */
#define RSTACK_SIZE 4096
#define STACK_SIZE 4096
.lcomm stack, STACK_SIZE
.lcomm rstack, RSTACK_SIZE

/* Pointer to the last word which was defined in the built-in wordlist. */
.lcomm builtin_wordlist, 8

.data

/* Format of dictionary's entries

                                                 32-bit adreess of the previous entry
                                                 in the wordlist.
                                               /
                         +----------------+ <--
                         |   0xffffffff   |
                         +---+------------+ <<<---------- Entry's descriptior pointer
  Size of the name  ->   | n |  name  | F | <--- Flags
                         +---+------------+
                         |      CFA       | <--- Code 32bit address where interpretation
                         +----------------+      semantic, it is to say, the code to be
                         |                |      executed lives. Therefore, we compile a
                         |   Parameter    |      call to this word as a call to the CFA
                         |     field      |      address.
                         |                |
                         +----------------+
                         |       ....     |
                         +----------------+
*/

#define F_IMMEDIATE 1
#define F_COMPILE_TIME 2

/* Like `BUILTIN_WORD_NAME', but the name of the word will
   be the symbol's identifier itself. */
#define BUILTIN_WORD(name) BUILTIN_WORD_NAME(name, # name)

/* Define a primitive word in the Forth environment. The native code
   should be enclosed between the BUILTIN_WORD_NAME and END_WORD macros. NAME
   is a symbol which could be used to call from assembler. STR is a string
   which stands for the name of the word in the Forth environment.
   Primitive words will be avalaible as part of the built-in wordlist. */
#define BUILTIN_WORD_NAME(name, str)            \
        BUILTIN_WORD_ENTRY(name, str, 0)

/* Define a word entry. Used to implement BUILTIN_WORD_NAME and BUILTIN_VARIABLE macros. */
#define BUILTIN_WORD_ENTRY(name, str, flag)     \
        begin_ ## name:                         \
        .byte end_ ## name ## _str - . - 1;     \
        .ascii str;                             \
        end_ ## name ## _str:                   \
        .byte flag;                             \
        name ## _cfa:                           \
        .long name;                             \
        .text;                                  \
        name:

/* End mark to built-in words.*/
#define END_WORD(name)                          \
        ret;                                    \
        .data;                                  \
        .long begin_ ## name


/* Define a primitive variable word. When it is executed, it pushes to
   the data stack the address of a reserved cell in memory. As above,
   SYMBOL is the assembler name, NAME is the name within the Forth
   environment. Finally, DEFAULT is the default value. */
#define BUILTIN_VARIABLE_NAME(symbol, name, default)    \
        BUILTIN_WORD_NAME(symbol ## _variable , name)   \
        subl $4, %esi;                                  \
        movl $symbol, (%esi);                           \
        ret;                                            \
        .data;                                          \
        symbol:                                         \
                .long default;                          \
        END_WORD(symbol ## _variable)

#define BUILTIN_VARIABLE(name, default)                 \
        BUILTIN_VARIABLE_NAME(name, # name, default)

/* Internal variables */

/* Buffer of the counted strings read by the parser. */
read_word_buffer:
        .fill 32


dictionary_start:

.long 0

/* Primitive Forth words

        WARNING:

        NOT TO WRITE ANYTHING BETWEEN TWO DEFINITIONS, THE DEFINITION MACROS ASSUME
        THERE IS A END_WORD PREVIOUSLY. IT IS WHY YOU CAN FIND THE .LONG 0 ABOVE.
        OTHERWISE, THE DICTIONARY STRUCTURE WILL BE CORRUPTED.
*/


/************************************ CORE ************************************/

BUILTIN_VARIABLE(stack_underflow_err_routine, 0)
BUILTIN_VARIABLE(stack_overflow_err_routine, 0)
BUILTIN_VARIABLE(unknown_word_err_routine, 0)
BUILTIN_VARIABLE(compile_only_err_routine, 0)

/* Interpreter's input sources */

/* The role of the following variables may vary according to the kind
   of tye input source which is being processed. The input source type
   is determined by the value of the variable INPUT_SOURCE_ID.

   If the input source is an in-memory buffer. Then the input buffer
   is set to cover all the buffer, so no refilling will be required.
   On the other hand, if the input source is an user input device,
   input buffer will be refilled by the routine `tib_fill_routine'. */


/* A value which determine what kind of source we are processing. The
   values are: 0 (input user device, as keyboard or serial), -1 (a
   in-memory buffer as a string being processed by evaluate). */
BUILTIN_VARIABLE(input_source_id, 0)

/* Address of the input buffer. */
BUILTIN_VARIABLE(input_buffer, 0)

/* Location in the input source. Useful for provide some debugging
   information when an error occurs. */
BUILTIN_VARIABLE(input_source_line, 1)
BUILTIN_VARIABLE(input_source_column, 0)

/* Offset of the parsing area in the input buffer. It is to say, the
   offset to the data which were not processed yet. */
BUILTIN_VARIABLE(input_buffer_in, 0)

/* Size of the input buffer. */
BUILTIN_VARIABLE(input_buffer_size, 0)

/* Routine which is called when the input source is a user input
   device and the input buffer is empty. */
BUILTIN_VARIABLE(tib_fill_routine, 0)


BUILTIN_WORD_NAME(_read_word_buffer, "read_word_buffer")
        subl $4, %esi
        movl $read_word_buffer, (%esi)
        ret
END_WORD(_read_word_buffer)


/* Search order stack */
#define SORDER_STACK_SIZE 64
.lcomm sorder_stack, SORDER_STACK_SIZE*4

/* Offset of the first element in the search order stack. */
BUILTIN_VARIABLE(sorder_tos, 0)


BUILTIN_WORD(sorder_size)
        subl $4, %esi
        movl $SORDER_STACK_SIZE, (%esi)
        ret
END_WORD(sorder_size)


BUILTIN_WORD_NAME(_sorder_stack, "sorder_stack")
        subl $4, %esi
        movl $sorder_stack, (%esi)
        ret
END_WORD(_sorder_stack)


BUILTIN_WORD(dp)
        subl $4, %esi
        movl %edi, (%esi)
        ret
END_WORD(dp)


BUILTIN_WORD_NAME(dp_store, "dp!")
        movl (%esi), %edi
        addl $4, %esi
        ret
END_WORD(dp_store)


BUILTIN_WORD_NAME(dp_base, "dp-base")
        subl $4, %esi
        movl $dictionary_start, (%esi)
        ret
END_WORD(dp_base)


/* Push the value of the stack pointer _before_ of executing this word. */
BUILTIN_WORD(sp)
        movl %esi, %eax
        subl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(sp)

/* Set the value of the stack pointer to the tos's value. */
BUILTIN_WORD_NAME(sp_store, "sp!")
        movl (%esi), %esi
        ret
END_WORD(sp_store)


BUILTIN_WORD_NAME(sp_base, "sp-base")
        subl $4, %esi
        movl $stack, (%esi)
        ret
END_WORD(sp_base)


BUILTIN_WORD_NAME(sp_limit, "sp-limit")
        subl $4, %esi
        movl $stack+STACK_SIZE, (%esi)
        ret
END_WORD(sp_limit)


BUILTIN_WORD(rsp)
        subl $4, %esi
        movl %esp, (%esi)
        /* Don't take in account the increment of the RSP pointer due to this word. */
        addl $4, (%esi)
        ret
END_WORD(rsp)


BUILTIN_WORD_NAME(rsp_store, "rsp!")
        /* KLUDGE: A fake RET is used here. We should
           include support for inlining soon. */
        popl %eax
        movl (%esi), %esp
        addl $4, %esi
        jmp *%eax
END_WORD(rsp_store)


BUILTIN_WORD_NAME(rsp_base, "rsp-base")
        subl $4, %esi
        movl $rstack, (%esi)
        ret
END_WORD(rsp_base)


BUILTIN_WORD_NAME(rsp_limit, "rsp-limit")
        subl $4, %esi
        movl $(rstack + RSTACK_SIZE), (%esi)
        ret
END_WORD(rsp_limit)


BUILTIN_WORD_NAME(gdt_cs_selector, "gdt-cs-selector")
        subl $4, %esi
        movl $GDT_CS_SELECTOR, (%esi)
        ret
END_WORD(gdt_cs_selector)


BUILTIN_WORD_NAME(store, "!")
        movl (%esi), %eax
        movl 4(%esi), %edx
        movl %edx, (%eax)
        addl $8, %esi
        ret
END_WORD(store)


BUILTIN_WORD_NAME(fetch, "@")
        movl (%esi), %eax
        movl (%eax),%edx
        movl %edx, (%esi)
        ret
END_WORD(fetch)


BUILTIN_WORD_NAME(w_store, "w!")
        movl (%esi), %eax
        movl 4(%esi), %edx
        movw %dx, (%eax)
        addl $8, %esi
        ret
END_WORD(w_store)


BUILTIN_WORD_NAME(w_fetch, "w@")
        xorl %edx, %edx
        movl (%esi), %eax
        movw (%eax), %dx
        movl %edx, (%esi)
        ret
END_WORD(w_fetch)


BUILTIN_WORD_NAME(c_store, "c!")
        movl (%esi), %eax
        movl 4(%esi), %edx
        movb %dl, (%eax)
        addl $8, %esi
        ret
END_WORD(c_store)


BUILTIN_WORD_NAME(c_fetch, "c@")
        xorl %edx, %edx
        movl (%esi), %eax
        movb (%eax), %dl
        movl %edx, (%esi)
        ret
END_WORD(c_fetch)


/* port -- n */
BUILTIN_WORD(inputb)
        xorl %eax, %eax
        movl (%esi), %edx
        inb %dx, %al
        movl %eax, (%esi)
        ret
END_WORD(inputb)

/* n port -- */
BUILTIN_WORD(outputb)
        movl  (%esi), %edx
        movl 4(%esi), %eax
        addl $8, %esi
        outb %al, %dx
        ret
END_WORD(outputb)


BUILTIN_WORD_NAME(io_wait, "io-wait")
	/* I don't like being rude but, WTF? */
        jmp 1f
1:      jmp 2f
2:      ret
END_WORD(io_wait)


BUILTIN_WORD(cli)
        cli
        ret
END_WORD(cli)


BUILTIN_WORD(sti)
        sti
        ret
END_WORD(sti)

/* SIDT and LIDT words use this location as temporary storage. */
.lcomm idtr, 6

/* Load IDT register from the stack. */
BUILTIN_WORD(lidt) /* addr limit -- */
	movl (%esi), %eax
        movw %ax, idtr
        movl 4(%esi), %eax
        movl %eax, idtr+2
        addl $8, %esi
        lidt idtr
        ret
END_WORD(lidt)


/* Save the IDT register into the stack. */
BUILTIN_WORD(sidt) /* -- addr limit */
        subl $8, %esi
        sidt idtr
        movl idtr+2, %eax
        movl %eax, 4(%esi)
        movzwl idtr, %eax
        movl %eax, 0(%esi)
        ret
END_WORD(sidt)


BUILTIN_WORD(halt)
        hlt
        ret
END_WORD(halt)


BUILTIN_WORD(cpuid)
        /* request -- %ecx %edx %ebx %eax  */
        movl (%esi), %eax
        cpuid
        subl $12, %esi
        movl %ecx, 12(%esi)
        movl %edx,  8(%esi)
        movl %ebx,  4(%esi)
        movl %eax,  0(%esi)
        ret
END_WORD(cpuid)


BUILTIN_WORD(eflags)
        pushf
        movl (%esp), %eax
        subl $4, %esi
        movl %eax, (%esi)
        addl $4, %esp
        ret
END_WORD(eflags)


BUILTIN_WORD_NAME(eflags_store, "eflags!")
        subl $4, %esp
        movl (%esi), %eax
        movl %eax, (%esp)
        addl $4, %esi
        popf
        ret
END_WORD(eflags_store)


BUILTIN_WORD_NAME(times, "*")
        movl 4(%esi), %eax
        movl  (%esi), %ecx
        addl $4, %esi
        imull %ecx
        movl %eax, (%esi)
        ret
END_WORD(times)


BUILTIN_WORD_NAME(plus, "+")
        movl (%esi), %eax
        addl $4, %esi
        addl %eax, (%esi)
        ret
END_WORD(plus)


BUILTIN_WORD_NAME(minus, "-")
        movl (%esi), %eax
        addl $4, %esi
        subl %eax, (%esi)
        ret
END_WORD(minus)


BUILTIN_WORD_NAME(divide, "/")
        movl 4(%esi), %eax
        cdq
        idivl (%esi)
	addl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(divide)


BUILTIN_WORD(mod)
        movl 4(%esi), %eax
        cdq
        idivl (%esi)
        addl $4, %esi
        movl %edx, (%esi)
        ret
END_WORD(mod)


BUILTIN_WORD(and)
        movl (%esi), %eax
        andl %eax, 4(%esi)
        addl $4, %esi
        ret
END_WORD(and)


BUILTIN_WORD(or)
        movl (%esi), %eax
        orl %eax, 4(%esi)
        addl $4, %esi
        ret
END_WORD(or)


BUILTIN_WORD(xor)
        movl (%esi), %eax
        xorl %eax, 4(%esi)
        addl $4, %esi
        ret
END_WORD(xor)


BUILTIN_WORD(invert)
        notl (%esi)
        ret
END_WORD(invert)


/* u n -- u */
BUILTIN_WORD(lshift)
        movl (%esi), %ecx
        addl $4, %esi
        shll %cl, (%esi)
        ret
END_WORD(lshift)


/* u n -- u */
BUILTIN_WORD(rshift)
        movl (%esi), %ecx
        addl $4, %esi
        shrl %cl, (%esi)
        ret
END_WORD(rshift)


BUILTIN_WORD_NAME(two_times, "2*")
        sall $1, (%esi)
        ret
END_WORD(two_times)


BUILTIN_WORD_NAME(two_divides, "2/")
        sarl $1, (%esi)
        ret
END_WORD(two_divides)
	

BUILTIN_WORD_ENTRY(to_r, ">r", F_COMPILE_TIME)
        /* Skip the stored %eip. */
        movl (%esi), %eax
        addl $4, %esi
        movl %esp, %ebp
	subl $4, %esp
        movl (%ebp), %edx
        movl %edx, -4(%ebp)
        movl %eax,   (%ebp)
        ret
END_WORD(to_r)


BUILTIN_WORD_ENTRY(r_from, "r>", F_COMPILE_TIME)
        subl $4, %esi
        movl 4(%esp), %eax
        movl %eax, (%esi)
        movl (%esp), %ebx
        movl %ebx, 4(%esp)
        addl $4, %esp
        ret
END_WORD(r_from)


BUILTIN_WORD_ENTRY(r_fetch, "r@", F_COMPILE_TIME)
        movl 4(%esp), %eax
        subl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(r_fetch)


BUILTIN_WORD_ENTRY(exit, "exit", F_COMPILE_TIME)
        addl $4, %esp
        ret
END_WORD(exit)


BUILTIN_WORD_ENTRY(to_r_2, "2>r", F_COMPILE_TIME)
        movl %esp, %ebp
        subl $8, %esp
        movl (%ebp), %eax
        movl %eax, -8(%ebp)
        movl 4(%esi), %eax
        movl %eax, (%ebp)
        movl (%esi), %eax
        movl %eax, -4(%ebp)
        addl $8, %esi
        ret
END_WORD(to_r_2)


BUILTIN_WORD_ENTRY(r_from_2, "2r>", F_COMPILE_TIME)
        subl $8, %esi
        movl 8(%esp), %eax
        movl %eax,  4(%esi)
        movl 4(%esp), %eax
        movl %eax,  0(%esi)
        movl (%esp), %eax
        movl %eax, 8(%esp)
        addl $8, %esp
        ret
END_WORD(r_from_2)


BUILTIN_WORD_ENTRY(r_fetch_2, "2r@", F_COMPILE_TIME)
        subl $8, %esi
        movl 8(%esp), %eax
        movl %eax,  4(%esi)
        movl 4(%esp), %eax
        movl %eax, (%esi)
        ret
END_WORD(r_fetch_2)


BUILTIN_WORD_NAME(less_than, "<")
        xorl %eax, %eax
        movl (%esi), %ebx
        cmpl %ebx, 4(%esi)
        jnl 1f
        not %eax
1:      movl %eax, 4(%esi)
        addl $4, %esi
        ret
END_WORD(less_than)


BUILTIN_WORD_NAME(uless_than, "u<")
        xorl %eax, %eax
        movl (%esi), %ebx
        cmpl %ebx, 4(%esi)
        jnb 1f
        not %eax
1:      movl %eax, 4(%esi)
        addl $4, %esi
        ret
END_WORD(uless_than)


BUILTIN_WORD_NAME(ugreater_than, "u>")
        xorl %eax, %eax
        movl (%esi), %ebx
        cmpl %ebx, 4(%esi)
        jna 1f
        not %eax
1:      movl %eax, 4(%esi)
        addl $4, %esi
        ret
END_WORD(ugreater_than)


BUILTIN_WORD_NAME(equals, "=")
        xorl %eax, %eax
        movl (%esi), %ebx
        cmpl %ebx, 4(%esi)
        jne 1f
        not %eax
1:      movl %eax, 4(%esi)
        addl $4, %esi
        ret
END_WORD(equals)


BUILTIN_WORD_NAME(greater_than, ">")
        xorl %eax, %eax
        movl (%esi), %ebx
        cmpl %ebx, 4(%esi)
        jng 1f
        not %eax
1:      movl %eax, 4(%esi)
        addl $4, %esi
        ret
END_WORD(greater_than)



/* w - */
BUILTIN_WORD(drop)
        addl $4, %esi
        ret
END_WORD(drop)


/* w1 w2 - w2 */
BUILTIN_WORD(nip)
        movl (%esi), %eax
        addl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(nip)


/* w - w w*/
BUILTIN_WORD(dup)
        movl (%esi), %eax
        subl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(dup)


/* w1 w2 - w1 w2 w1 */
BUILTIN_WORD(over)
        movl 4(%esi), %eax
        subl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(over)


/* w1 w2 - w2 w1*/
BUILTIN_WORD(swap)
        movl  (%esi), %eax
        movl 4(%esi), %ebx
        movl %eax, 4(%esi)
        movl %ebx,  (%esi)
        ret
END_WORD(swap)


/* w1 w2 w3 - w2 w3 w1 */
BUILTIN_WORD(rot)
        movl  (%esi), %eax
        movl 4(%esi), %ebx
        movl 8(%esi), %ecx
        movl %eax, 4(%esi)
        movl %ebx, 8(%esi)
        movl %ecx,  (%esi)
        ret
END_WORD(rot)


BUILTIN_WORD(execute)
        movl (%esi), %eax
        addl $4, %esi
        call *%eax
        ret
END_WORD(execute)


BUILTIN_WORD(jump)
        movl (%esi), %eax
        addl $4, %esi
        addl $4, %esp
        jmp *%eax
END_WORD(jump)


/********************************** INTERPRETER **********************************/

/* Parse a word from the input stream and push the counted string.
   The counted string will valid while no other words are read. */
BUILTIN_WORD_NAME(parse_cname, "parse-cname")
        call read_word
        subl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(parse_cname)


/* Find the first avalaible NT whose name is the counted string on the stack. */
BUILTIN_WORD_NAME(find_cname, "find-cname")
        movl (%esi), %eax
        call find_word
        movl %edx, (%esi)
        ret
END_WORD(find_cname)


BUILTIN_WORD_ENTRY(paren, "(", F_IMMEDIATE)
        call fgetchar
        cmp $')', %al
        jne paren
        ret
END_WORD(paren)


BUILTIN_WORD_ENTRY(backslash, "\\", F_IMMEDIATE)
        call fgetchar
        cmp $10, %eax
        je 1f
        cmp $0, %eax
        je 1f
        jmp backslash
1:      ret
END_WORD(backslash)


BUILTIN_VARIABLE(base, 10)


BUILTIN_WORD_NAME(parse_char, "parse-char")
        call fgetchar
        subl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(parse_char)


BUILTIN_WORD_NAME(peek_char, "peek-char")
        call fpeekchar
        subl $4, %esi
        movl %eax, (%esi)
        ret
END_WORD(peek_char)
        

/********************************** COMPILER **********************************/

/* If it is not null, the following call to read_word will return that
   address indeed of read from the input stream. It could point to an
   empty string, which is used to create anonymous words. */
BUILTIN_VARIABLE(compiling_nextname, 0)

/* Last word defined. Note that it could be anonymous or non-accesible
   from the current search order. */
BUILTIN_VARIABLE(latest_word, 0)


BUILTIN_WORD(header)
        call cread_word
        movl $1f, %edx
        call create_uninterned_word
1:      ret
END_WORD(header)


BUILTIN_WORD(reveal)
        movl latest_word, %eax
        movb (%eax), %cl
        test %cl, %cl
        jz 1f
        movl current, %ebp
        movl %eax, (%ebp)
1:      ret
END_WORD(reveal)


BUILTIN_WORD_NAME(colon, ":")
        movl $-1, state
        call cread_word
        movzbl (%eax), %ecx
        movl %ecx, %edx
        addl %edi, %edx
        addl $10, %edx
        call create_uninterned_word
1:      ret
END_WORD(colon)


BUILTIN_WORD_ENTRY(semicolon, ";", F_IMMEDIATE | F_COMPILE_TIME)
        /* Compile a RET into dictionary and set up interpretation mode */
        movb $0xC3, (%edi) /* RET */
        incl %edi
        movl $0, state
        call reveal
        ret
END_WORD(semicolon)


BUILTIN_VARIABLE(state, 0)


/* Search order.

   The representation for a wordlist is the following:

   WID -->  +--------------+
            |    latest    | <-- NT of the last word defined in this wordlist
            +--------------+
            |    method    | <-- XT of a word which finding words in the wordlist
            +--------------+     relies on. It takes a counted-string from the
                                 data stack and push associated NT, or 0 if it is
                                 not found. It could also be zero to indicate that
                                 the default search method will be used.

   The wordlist's identifier (WID) is a pointer to that structure.
*/

BUILTIN_VARIABLE(current, 0)

BUILTIN_WORD(evaluate)
        /* Save input */
        pushl input_buffer
        pushl input_buffer_in
        pushl input_buffer_size
        pushl input_source_id
        pushl input_source_line
        pushl input_source_column
        /* Change input source*/
        movl $-1, input_source_id
        movl 4(%esi), %eax
        movl %eax, input_buffer
        movl $0, input_buffer_in
        movl 0(%esi), %eax
        movl %eax, input_buffer_size
        movl $1, input_source_line
        movl $0, input_source_column
        addl $8, %esi
        /* Process */
        call interpret
        /* Restore input */
        popl input_source_column
        popl input_source_line
        popl input_source_id
        popl input_buffer_size
        popl input_buffer_in
        popl input_buffer
        ret
END_WORD(evaluate)

/* Yield the control to the interpreter in order to process the input
   buffer. This is intended to very low-level code, as we can wrap a
   loop interation with exceptions handlers and so. */
BUILTIN_WORD(interpret)
        call loop
        ret
END_WORD(interpret)


BUILTIN_VARIABLE(multiboot_addr, 0)


/* This file defines a word @NAME for each built-in Forth file, whose
   semantic is pushing the data of the file's location in memory
   and so, `evaluate' can process it. */
#include "BUILTIN-FILES.S"


dictionary_end:
  /* ...                            */
  /* Free space to user definitions */
  /* ...                             */



/*******************************************************************/

.text

/* Read a character from input_buffer.
   If it is empty, then it will try to refill it before. */
fgetchar:
        call fpeekchar
        incl input_buffer_in
        ret

/* Read a character from input_buffer without using it.
   If it is empty, then it will try to refill it before. */
fpeekchar:
        movl input_buffer_in, %edx
        cmpl %edx, input_buffer_size
        je fpeekchar_last
        jle fpeekchar_eob
        addl input_buffer, %edx
        movzbl (%edx), %eax
        /* Update input source line and column */
        incl input_source_column
        cmp $10, %eax
        jne 1f
        incl input_source_line
        movl $0, input_source_column
1:      ret
fpeekchar_last:
        movl $'\n', %eax
        ret
fpeekchar_eob:
        /* End-of-buffer. The input buffer is empty. It may be due to
	   all memory buffer has been processed, or the user did not
           type more data yet. INPUT_SOURCE_ID is used to know the
           reason of that. */
        movl input_source_id, %edx
        cmpl $-1, %edx
        je fpeekchar_eof
        /* The TIB is empty, so return a delimitier (space) and refill
           the user input buffer for the next time. */
        call *tib_fill_routine
        jmp fpeekchar
fpeekchar_eof:
        xorl %eax, %eax
        ret
	

        
/* Check if two given counted strings are equal. EAX y EDX point to the strings,
   the result is stored in the ZF flag. */
wordname_match:
        movb (%eax), %cl
1:      movb (%eax, %ecx), %bl
        cmpb %bl, (%edx, %ecx)
        jne 2f
        test %cl, %cl
        je 2f
        decb %cl
        jmp 1b
2:      ret

/* Read a word from the input stream. Push the countd string in the EAX register.
   This string is `lowcased' automatically. The version `cread_word' is used by
   functions which create words in the dictionary, in order to support nextname
   and anonymous words. */
cread_word:
        movl compiling_nextname, %eax
        test %eax, %eax
        jz read_word
        movl $0, compiling_nextname
        ret
read_word:
        call fgetchar
	/* Pay attention to the EOF. */
        test %al, %al
        jnz 0f
        xorl %eax, %eax
        ret
        /* Skip delimiter characters */
0:      cmpb $32, %al
        je read_word
        cmpb $10, %al
        je read_word
        cmpb $13, %al
        je read_word
        cmpb $9, %al
        je read_word
/* Read the word */
        pushl %ebx
        leal read_word_buffer, %ebx
        movb $31, %cl
1:
	cmpb $0, %al
        je 4f
        cmpb $32, %al
        je 4f
        cmpb $10, %al
        je 4f
        cmpb $13, %al
        je 4f
        cmpb $9, %al
        je 4f
        testb %cl, %cl
        jz 3f
        decb %cl
        incl %ebx
        cmpb $'A', %al
        jl 2f
        cmpb $'Z', %al
        jg 2f
        subb $('A' -  'a'), %al
2:
        movb %al, (%ebx)
3:
        pushl %ecx
        pushl %ebx
        call fgetchar
        popl %ebx
        popl %ecx
        jmp 1b
4:
        movb $31, read_word_buffer
        subb %cl, read_word_buffer
        /* Push the address to the stack */
        movl $read_word_buffer, %eax
        popl %ebx
        ret


/* Create a new entry in the dictionary, EAX is the counted string and EDX
   is the native code address. This word is not interned in any wordlist,
   however. The word is not interned if it is empty, therefore it creates
   an anonymous word. */
create_uninterned_word:
        /* Address of the previous entry */
        movl current, %ebp
        movl (%ebp), %ecx
        movl %ecx, (%edi)
        addl $4, %edi
	/* Address of the word (NT) */
        pushl %edi
        /* Name */
        movzbl (%eax), %ecx
        incl %ecx
        pushl %esi
        movl %eax, %esi
        cld
        rep movsb
        pop %esi
        /* Flag */
        movb $0, (%edi)
        incl %edi
        /* Compilation code */
        movl %edx, (%edi)
        addl $4, %edi
        popl %eax
        movl %eax, latest_word
        ret

/* Create a new entry in the wordlist, EAX is a counted string, EDX is the address of
   the native code. Return the address of the word in the EAX register. */
create_word:
        movb (%eax), %cl
        pushl %ecx
        call create_uninterned_word
        popl %ecx
        test %ecx, %ecx
        jz 1f
        movl current, %ebp
        movl %eax, (%ebp)
1:      ret

/* Find the counted word in EAX in the wordlist in EDX using the
   provide search method or the default if it is not specified. */
find_word_in_wordlist:
        movl 4(%edx), %ebx
        test %ebx, %ebx
        jnz find_word_in_wordlist
        movl (%edx), %edx
0:      test %edx, %edx
        jz 1f
        call wordname_match
        je 1f
        movl -4(%edx), %edx
        jmp 0b
1:      ret

find_word_in_wordlist_custom:
        subl $8, %esi
        movl %eax, (%esi)
        movl %edx, 4(%esi)
        call *%ebx
        movl (%esi), %edx
        addl $4, %esi
        ret

/* Find the counted word in EAX in the search order.
   Return the NT in EDX or 0 if it is not found. */
find_word:
        movl sorder_tos, %ecx
1:      movl sorder_stack(, %ecx, 4), %edx
        pushl %ecx
        call find_word_in_wordlist
        popl %ecx
        test %edx, %edx
        jnz 2f
        decl %ecx
        cmpl $-1, %ecx
        jne 1b
2:      ret


/* NUMERIC WORDS PARSING */

/* BINARY */
number2:
        push %ebx
        xorl %eax, %eax
        movzbl (%edx), %ecx
1:      test %ecx, %ecx
        jz 3f
        decl %ecx
        incl %edx
        movzbl (%edx), %ebx
        cmpl $'0', %ebx
        jl 2f
        cmpl $'1', %ebx
        jg 2f
        subl $'0', %ebx
        shll $1, %eax
        addl %ebx, %eax
        jmp 1b
2:
        xorl %eax, %eax
        xorl %edx, %edx
3:      popl %ebx
        ret


/* OCTAL */
number8:
        pushl %ebx
        xorl %eax, %eax
        movzbl (%edx), %ecx
1:
        test %ecx, %ecx
        jz 3f
        decl %ecx
        incl %edx

        pushl %edx
        movl $8, %ebx
        mull %ebx
        popl %edx
        movzbl (%edx), %ebx

        cmpl $'0', %ebx
        jl 2f
        cmpl $'7', %ebx
        jg 2f
        subl $'0', %ebx
        addl %ebx, %eax
        jmp 1b
2:
        xorl %eax, %eax
        xorl %edx, %edx
3:
        pop %ebx
        ret


/* DECIMAL */
number10:
        pushl %ebx
        xorl %eax, %eax
        movzbl (%edx), %ecx
1:
        test %ecx, %ecx
        jz 3f
        decl %ecx
        incl %edx

        pushl %edx
        movl $10, %ebx
        mull %ebx
        popl %edx
        movzbl (%edx), %ebx

        cmpl $'0', %ebx
        jl 2f
        cmpl $'9', %ebx
        jg 2f
        subl $'0', %ebx
        addl %ebx, %eax
        jmp 1b
2:
        xorl %eax, %eax
        xorl %edx, %edx
3:
        pop %ebx
        ret

/* HEXADECIMAL */
number16:
        pushl %ebx
        xorl %eax, %eax
        xorl %ecx, %ecx
        movzbl (%edx), %ecx
1:
        test %ecx, %ecx
        jz 5f
        decl %ecx
        incl %edx

        pushl %edx
        movl $16, %ebx
        mull %ebx
        popl %edx
        movzbl (%edx), %ebx
        cmpl $'a', %ebx
        je 3f
        cmpl $'b', %ebx
        je 3f
        cmpl $'c', %ebx
        je 3f
        cmpl $'d', %ebx
        je 3f
        cmpl $'e', %ebx
        je 3f
        cmpl $'f', %ebx
        je 3f
        cmpl $'0', %ebx
        jl 4f
        cmpl $'9', %ebx
        jg 4f
        subl $'0', %ebx
2:
        addl %ebx, %eax
        jmp 1b
3:
        subl $('a'-10), %ebx
        addl %ebx, %eax
        jmp 1b
4:
        xorl %eax, %eax
        xorl %edx, %edx
5:
        pop %ebx
        ret

/* Read an integer according to the BASE variable, from the
   counted input stream in the %EDX register. If it is not a number,
   EDX=0. Otherwise, EDX=1 and the numeric value is returned in %EAX. */
number:
        movb 1(%edx), %bl
        cmp $'-', %bl
        jne 1f
        call number_skip_char
1:
        pushl base
        call number_override_base

        cmpb $2, base
        je 2f
        cmpb $8, base
        je 8f
        cmpb $10, base
        je 10f
        cmpb $16, base
        je 16f

2:      call number2
        jmp number_sign
8:      call number8
        jmp number_sign
10:     call number10
        jmp number_sign
16:     call number16

number_sign:
        popl base
        cmp $'-', %bl
        jne 3f
        neg %eax
3:
        ret


number_skip_char:
        pushl %ebx
        movb (%edx), %bh
        decb %bh
        movb %bh, 1(%edx)
        incl %edx
        popl %ebx
        ret

number_override_base:
        push %ebx
        movb 1(%edx), %bl
        cmp $'&', %bl
        je 10f
        cmp $'#', %bl
        je 10f
        cmp $'%', %bl
        je 2f
        cmp $'$', %bl
        je 16f
        jmp 3f
2:      movb $2, base
        call number_skip_char
        jmp 3f
10:     movb $10, base
        call number_skip_char
        jmp 3f
16:     movb $16, base
        call number_skip_char
        jmp 3f
3:      popl %ebx
        ret


.globl run_forth
run_forth:
        cmp $0x2BADB002, %eax
        jne nomultiboot
        movl %ebx, multiboot_addr
nomultiboot:
        cli
        movl $(rstack+RSTACK_SIZE), %esp
        movl $(stack + STACK_SIZE), %esi
        movl $_end, %edi
        /* Set up a flat model of memory */
        call flush_gdt
        /* Initialize the built-in wordlist */
        movl (dictionary_end-4), %eax
        movl %eax, builtin_wordlist
        movl $0, builtin_wordlist+4
        movl $builtin_wordlist, sorder_stack
        movl $builtin_wordlist, current
        /* Load core.fs */
        subl $8, %esi
        movl $_binary_core_fs_start, 4(%esi)
        movl $_binary_core_fs_size,  0(%esi)
        call evaluate
        /* This point will not should be reached. */
        cli
        hlt

/* Main loop. Read word and run the compiler or the interpreter,
   according to the STATE's value. */
loop:
        call read_word
        test %eax, %eax
        jnz 1f
        ret
1:      pushl %eax
        call find_word
        test %edx, %edx
        jnz loop_word
        /* Try to parse a number */
        popl %edx
        call number
        test %edx, %edx
        jnz loop_number
        /* Throw an error */
        call *unknown_word_err_routine
        ret
loop_word:
        /* EDX = Word in the dictionary */
        popl %eax           /* String */
        pushl %edx
        movzbl (%edx), %ecx
        addl %ecx, %edx
        incl %edx
        movzbl (%edx), %eax /* Flags */
        test $F_IMMEDIATE, %eax
        popl %edx
        jnz interpret_word
        movl state, %eax
        test %eax, %eax
        jnz compile_word
        jmp interpret_word
loop_number:
        /* EAX = Number */
        movl state, %edx
        test %edx, %edx
        jz interpreter_number
        jmp compile_number

compile_word:
        /* EDX = Entry in the dictionary */
        /* Find the CFA of the word and yield to %EDX. */
        /* CALL $X = E8 rel32 */
        movzbl (%edx), %ecx
        addl %ecx, %edx
        addl $2, %edx
        movl (%edx), %edx
        /* Save the value into dictionary. */
        movb $0xe8, (%edi)
        incl %edi
        subl %edi, %edx
        subl $4, %edx
        movl %edx, (%edi)
        addl $4, %edi
        jmp loop

compile_number:
        /* EAX = Number */
        /* subl $4, %esi = 83 EE 04 */
        movb $0x83, 0(%edi)
        movb $0xEE, 1(%edi)
        movb $0x04, 2(%edi)
        /* movl $X, (%esi) = C7 06 X */
        movb $0xC7, 3(%edi)
        movb $0x06, 4(%edi)
        movl %eax,  5(%edi)
        addl $9, %edi
        jmp loop

interpret_word:
        /* EDX = Dictionary's entry */
        movzbl (%edx), %ecx
        addl %ecx, %edx
        incl %edx
        /* On interpreting, so check it is not a compile-only word. */
        movl state, %eax
        test %eax, %eax
        jnz 1f
        movzbl (%edx), %eax     /* flags */
        test $F_COMPILE_TIME, %eax
        jz 1f
        call *compile_only_err_routine
        jmp loop

1:      incl %edx
        movl (%edx), %eax
        addl $4, %edx
        /* EAX = word's CFA */
        /* EDX = word's PFA */
        call *%eax
        call check_stack_consistency
        jmp loop

interpreter_number:
        /* EAX = Number */
        subl $4, %esi
        movl %eax, (%esi)
        jmp loop

check_stack_consistency:
        cmpl $(stack+STACK_SIZE), %esi
        jle 1f
        call *stack_underflow_err_routine
1:      cmpl $(stack), %esi
        jge 2f
        call *stack_overflow_err_routine
2:      ret

/*
Local variables:
coding: utf-8
indent-tabs-mode: nil
End:
*/

/* forth.S ends here */
