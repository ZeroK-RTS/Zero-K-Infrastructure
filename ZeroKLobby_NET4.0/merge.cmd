rem ilmerge /out:c:\inetpub\wwwroot\download\CaDownloader.exe CaDownloader.exe CaUpdaterLib.dll ICSharpCode.SharpZipLib.dll MonoTorrent.dll SpringLib.dll MyDownloader.Core.dll MyDownloader.Extension.dll
rem mt -manifest "../caupdater/cadownloader.exe.manifest" -outputresource:"c:\inetpub\wwwroot\download\CaDownloader.exe"

cd c:\work\other\ZeroKLobby\bin\debug
ILMerge.exe /out:zkl.exe "Zero-K.exe" "*.dll" /target:exe "/targetplatform:v4,C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client" /wildcards
