cd bin
cd Release
del *.pdb

ilmerge /out:../../LuaAdmin.exe LuaAdmin.exe LuaManagerLib.dll

cd ..
cd ..