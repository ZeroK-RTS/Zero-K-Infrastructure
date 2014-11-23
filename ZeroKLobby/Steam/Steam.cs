using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlasmaShared;

namespace ZeroKLobby.Steam
{
    public class Steam
    {
        static Steam()
        {
            EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.CSteamworks.dll", "CSteamworks.dll");
            EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.steam_api.dll", "steam_api.dll");
            EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.steam_appid.txt", "steam_appid.txt");
        }



    }
}
