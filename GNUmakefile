# -*- makefile -*-
#

.PHONY: all clean dist.

KERNEL=eulex

all: $(KERNEL)

LINKER_SCRIPT = eulex.lds

CFLAGS = -fstrength-reduce -nostdinc -m32 -nostdlib -fno-builtin -nostartfiles -nodefaultlibs -I. -ggdb
ASFLAGS = $(CFLAGS) -I.
DEPEND_FLAGS=-MM
LDFLAGS=-Wl,-T$(LINKER_SCRIPT)
FORTH_SRC= \
	core.fs \
        exceptions.fs \
	output.fs \
	string.fs \
        math.fs \
	tools.fs \
        disassem.fs \
        structures.fs \
        interpreter.fs \
        kernel/multiboot.fs \
        kernel/cpuid.fs \
	kernel/video.fs \
        kernel/console.fs \
        kernel/interrupts.fs \
        kernel/exceptions.fs \
        kernel/irq.fs \
        kernel/timer.fs \
        kernel/keyboard.fs \
        kernel/speaker.fs \
	kernel/serial.fs \
	linedit.fs \
	user.fs \
	eulexrc.fs

TESTING_SRC=tests/tests.fs \
	    tests/tsuite.fs \
            tests/base.fs \
	    tests/strings.fs

ASM_SRC=boot.S forth.S

SOURCES=$(ASM_SRC)
HEADERS=multiboot.h

OBJS = $(ASM_SRC:.S=.o) $(FORTH_SRC:.fs=.o) $(TESTING_SRC:.fs=.o)

$(KERNEL): $(OBJS) $(LINKER_SCRIPT)
	$(CC) $(CFLAGS) $(LDFLAGS) -o $@ $^

clean:
	-rm -f *.[do] kernel/*.[do] tests/*.[do] $(KERNEL) BUILTIN-FILES.S

%.d: %.S GNUmakefile
	@$(CC) $(DEPEND_FLAGS) $(CPPFLAGS) $< > $@.tmp; \
	sed 's,\($*\)\.o[ :]*,\1.o $@ : ,g' < $@.tmp > $@; \
	rm -f $@.tmp

%.o: %.fs
	objcopy -I binary -O elf32-i386 -Bi386 $< $@

eulexrc.fs:
	echo "( Write your personal definitions in this file )" > $@

forth.S: BUILTIN-FILES.S
BUILTIN-FILES.S: GNUmakefile
	sh ./generate-builtin-files.sh $(FORTH_SRC) $(TESTING_SRC)

dist:
	git archive --format=tar --prefix=eulex/ HEAD | gzip > eulex.tar.gz


# ifneq ($(MAKECMDGOALS),clean)
# -include $(ASSEM_SRC:.S=.d)
# endif
