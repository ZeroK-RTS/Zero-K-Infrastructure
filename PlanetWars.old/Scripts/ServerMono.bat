set %OLDDIR% = %CD%
chdir ..\bin
"%PROGRAMFILES%\Mono-2.0\bin\mono.exe" PlanetWarsServer.exe
chdir %OLDDIR%