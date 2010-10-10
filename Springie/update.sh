#!/bin/sh

cd /home/springie/springie

if [ Springie2.exe -nt Springie.exe ] && [ -z "`ps h -C spring-dedicated`" ]; 
    then
    rm Springie.exe;
    mv Springie2.exe Springie.exe;
    killall mono;
    mono Springie.exe 2>&1 > springie_log.txt & disown;
    fi