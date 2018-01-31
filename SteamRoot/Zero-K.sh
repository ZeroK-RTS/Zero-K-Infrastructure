#!/bin/sh
installdir=$( dirname "${0}" )
cd "$installdir"
if [ -f "$installdir/Zero-K.exe" ]
    then
    mono Zero-K.exe "$@"
    else
    if [ -f "$installdir/setup-zerok.sh" ]
        then
        /bin/bash setup-zerok.sh "$@"
        else
        zenity --info --title "Error!" --text "Zero-K installation file was corrupted\! Please remove installation and redownload."
    fi
fi
