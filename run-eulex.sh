#! /bin/sh

# TODO: Add checks for qemu installation
KERNEL=forth.core
qemu -soundhw pcspk -s -serial stdio -net none -kernel $KERNEL

# run-eulex.sh ends here
