#!/bin/sh

ln -fs /lib/x86_64-linux-gnu/libc.so.6 linux64/libc.so

export LD_LIBRARY_PATH="./linux64:$LD_LIBRARY_PATH"
export TERM=xterm
./Zero-K_linux64 "$@"
