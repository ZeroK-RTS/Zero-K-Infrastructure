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

        List<ulong> friends = new List<ulong>();

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
                friends = SteamApi.GetFriends();
                SteamID = SteamApi.GetSteamID();
                if (tas.IsLoggedIn && tas.MyUser!=null && tas.MyUser.EffectiveElo != 0) OnLoggedToBothSteamAndTas();
            };

            tas.MyExtensionsChanged += (sender, args) => { if (SteamApi.IsOnline && SteamID != 0) OnLoggedToBothSteamAndTas(); };
            tas.UserExtensionsChanged += (sender, args) =>
            {
                if (args.Data.SteamID != null && SteamID != 0 &&  friends.Contains(args.Data.SteamID.Value))
                {
                    AddFriend(args.Data.Name);
                }
            };
        }

        static void AddFriend(string name)
        {
            Program.MainWindow.InvokeFunc(() => Program.FriendManager.AddFriend(name));
        }

        void OnLoggedToBothSteamAndTas()
        {
            if (tas.MyUser.SteamID == null) tas.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, string.Format("!linksteam {0}", SteamApi.GetClientAuthTokenHex()), false);
            foreach (var u in tas.ExistingUsers.Values.ToList().Where(x => x.SteamID != null && friends.Contains(x.SteamID.Value)))
            {
                AddFriend(u.Name);
            }
        }

        public void Connect()
        {
            SteamApi.ConnectToSteam();
        }
    }
}
