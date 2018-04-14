#!/bin/sh

ln -fs /lib/i386-linux-gnu/libc.so.6 linux32/libc.so

export LD_LIBRARY_PATH="./linux32:$LD_LIBRARY_PATH"
./Zero-K_linux32 $@
