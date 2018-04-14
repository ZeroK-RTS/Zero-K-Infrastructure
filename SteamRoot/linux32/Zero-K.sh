#!/bin/sh

ln -fs /lib//i386-linux-gnu/libc.so.6 libc.so

export LD_LIBRARY_PATH=".:$LD_LIBRARY_PATH"
./Zero-K_linux32
