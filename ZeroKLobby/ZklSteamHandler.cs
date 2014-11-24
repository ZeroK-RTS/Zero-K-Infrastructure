using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZeroKLobby
{
    public class ZklSteamHandler
    {
        TasClient tas;
        public SteamInterface SteamApi { get; private set; }

        public string SteamName { get; private set; }
        public ulong SteamID { get; private set; }

        public ZklSteamHandler(TasClient tas)
        {
            this.tas = tas;
            EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.CSteamworks.dll", "CSteamworks.dll");
            EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.steam_api.dll", "steam_api.dll");
            EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.steam_appid.txt", "steam_appid.txt");
            SteamApi = new SteamInterface(GlobalConst.SteamAppID);
            SteamApi.SteamOnline += () =>
            {
                SteamName = SteamApi.GetMyName();
                SteamID = SteamApi.GetSteamID();
                if (tas.IsLoggedIn && tas.MyUser.EffectiveElo != 0 && tas.MyUser.SteamID == null) tas.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, string.Format("!linksteam {0}", SteamApi.GetClientAuthTokenHex()), false);
            };

            tas.MyExtensionsChanged += (sender, args) =>
            {
                if (SteamApi.IsOnline && args.Data.SteamID == null) tas.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, string.Format("!linksteam {0}", SteamApi.GetClientAuthTokenHex()), false);
            };
        }

        public void Connect()
        {
            SteamApi.ConnectToSteam();
        }
    }
}
