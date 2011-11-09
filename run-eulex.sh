#! /bin/sh

# TODO: Add checks for qemu installation
KERNEL=eulex
qemu -soundhw pcspk -s -serial stdio -kernel $KERNEL

# run-eulex.sh ends here
