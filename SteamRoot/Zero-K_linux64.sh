#!/bin/sh
TERM=xterm
if [ -f /lib/x86_64-linux-gnu/libc.so.6 ]; then
    ln -fs /lib/x86_64-linux-gnu/libc.so.6 linux64/libc.so
elif [ -f /lib64/libc.so.6 ]; then
    ln -fs /lib64/libc.so.6 linux64/libc.so
elif [ -f /lib/libc.so.6 ]; then
    ln -fs /lib/libc.so.6 linux64/libc.so
fi

export LD_LIBRARY_PATH="./linux64:$LD_LIBRARY_PATH"
./Zero-K_linux64 "$@"
