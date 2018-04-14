#!/bin/sh

ln -fs /lib/x86_64-linux-gnu/libc.so.6 libc.so

export LD_LIBRARY_PATH=".:$LD_LIBRARY_PATH"
./Zero-K_linux64 
