#!/bin/sh
installdir=$( dirname "${0}" )
cd "$installdir"
if [ -f "$installdir/installed.txt" ]
    then
    mono Zero-K.exe "$@"
else
    pkgmanager=$( which apt-get )
    pkx=$( which pkexec )
    notify-send "Zero-K is installing required packages, please wait.. "
    sleep 1
    notify-send "The game will launch once the installation is complete."
    ${pkx} ${pkgmanager} -y install mono-complete libsdl2-2.0-0 libopenal1 libcurl3 zenity libgdiplus sqlite3 && touch installed.txt

    mono Zero-K.exe "$@"
fi 
