#!/bin/bash
installdir=$( dirname "${0}" )

# Setup dependencies ...
pkgmanager=$( which apt-get )
pkx=$( which pkexec )
if [ -n "${pkgmanager}" -a -n "${pkx}" ]
then
  # We know how to install packages on this system.
  # Can we show a prompt using zenity?
  which zenity; if [ $? = 0 ]
  then
    zenity --question --title "Install Dependencies" --text "Zero-K needs SDL2 and other dependencies to run.\nCheck for dependencies now?"
    answer=$?
    if [ $answer = 0 ]
    then
      ${pkx} ${pkgmanager} -y install mono-complete libsdl2-2.0-0 libopenal1 libcurl3 zenity libgdiplus sqlite3
    fi
  else
    # No zenity. Can we use the default terminal?
    altterm="/etc/alternatives/x-terminal-emulator"
    if [ -f "$altterm" ]
    then
      $altterm -e "bash -c \"echo 'We need to install some deps. (y/n)?' ; read answer ; if [ \$answer == \"y\" ]; then ${pkx} ${pkgmanager} -y install mono-complete libsdl2-2.0-0 libopenal1 libcurl3 zenity libgdiplus sqlite3; fi\""
    else
      # Can we use xterm?
      which xterm; if [ $? = 0 ]
      then
        xterm -e "bash -c 'echo \"We need to install some deps. (y/n)?\" ; read answer ; if [ \$answer == \"y\" ]; then ${pkx} ${pkgmanager} -y install mono-complete libsdl2-2.0-0 libopenal1 libcurl3 zenity libgdiplus sqlite3; fi'"
      fi
    fi
  fi
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


