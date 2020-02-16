#!/bin/sh
export LD_LIBRARY_PATH="./linux64:$LD_LIBRARY_PATH"
export TERM=xterm
./Zero-K_linux64 "$@"
