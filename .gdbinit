target remote localhost:1234
display /i $pc
python
import struct
with open("eulex.core", "rb") as f:
    header = False
    while not header:
        value, = struct.unpack("i", f.read(4))
        header = (value == 0x1BADB002)
    f.read(24) # discard other fields
    ep, = struct.unpack("i", f.read(4))
    bp = gdb.Breakpoint("*" + hex(ep))
    bp.enabled = True
end

