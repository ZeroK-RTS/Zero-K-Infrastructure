#!/bin/bash
installdir=$( dirname "${0}" )

# Setup dependencies ...
pkgmanager=$( which apt-get )
pkx=$( which pkexec )
if [ -n "${pkgmanager}" -a -n "${pkx}" ]
then
  ${pkx} ${pkgmanager} -y install mono-complete libsdl2-2.0-0 libopenal1 libcurl3 zenity libgdiplus sqlite3
fi

# Binary name
bin="Zero-K.exe"

# Setup ZK...
cd "${installdir}"
installdir=$PWD
wget -N https://zero-k.info/lobby/${bin} 2>&1 | tee /dev/stderr | sed -u "s/^ *[0-9]*K[ .]*\([0-9]*%\).*/\1/" | zenity --progress --text "Downloading Zero-K Lobby..." --title "Downloading Zero-K" --auto-close --auto-kill --no-cancel
chmod +x ${bin}

zenity --info --title "Done!" --text "Zero-K is now installed\!"
mono Zero-K.exe "$@"


