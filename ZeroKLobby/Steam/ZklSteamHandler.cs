using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby.Steam;
using ZkData;

namespace ZeroKLobby
{
    public class ZklSteamHandler : IDisposable
    {
        List<ulong> friends = new List<ulong>();
        readonly TasClient tas;
        public SteamClientHelper SteamHelper { get; private set; }
        SteamVoiceSystem steamVoice = new SteamVoiceSystem();

        public ulong SteamID { get; private set; }
        public string SteamName { get; private set; }

        public ZklSteamHandler(TasClient tas)
        {
            this.tas = tas;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.libCSteamworks.so", "libCSteamworks.so");
                EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.libsteam_api.so", "libsteam_api.so");
            }
            else
            {
                EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.CSteamworks.dll", "CSteamworks.dll");
                EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.steam_api.dll", "steam_api.dll");
            }
            EmbeddedResourceExtractor.ExtractFile("ZeroKLobby.NativeLibs.steam_appid.txt", "steam_appid.txt");

            SteamHelper = new SteamClientHelper();
            SteamHelper.SteamOnline += () =>
            {
                SteamName = SteamHelper.GetMyName();
                friends = SteamHelper.GetFriends();
                SteamID = SteamHelper.GetSteamID();
                if (tas.IsLoggedIn && tas.MyUser != null && tas.MyUser.EffectiveElo != 0) OnLoggedToBothSteamAndTas();
            };

            tas.MyExtensionsChanged += (sender, args) => { if (SteamHelper.IsOnline && SteamID != 0) OnLoggedToBothSteamAndTas(); };
            tas.UserExtensionsChanged += (sender, args) =>
            {
                if (args.Data.SteamID != null && SteamID != 0 && friends.Contains(args.Data.SteamID.Value)) AddFriend(args.Data.Name);
            };
        }

        public void Dispose()
        {
            if (SteamHelper != null)
            {
                try
                {
                    SteamHelper.Dispose();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        public void Connect()
        {
            try
            {
                SteamHelper.ConnectToSteam();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }


        static void AddFriend(string name)
        {
            Program.MainWindow.InvokeFunc(() => Program.FriendManager.AddFriend(name));
        }

        void OnLoggedToBothSteamAndTas()
        {
            if (tas.MyUser.SteamID == null)
            {
                string token = SteamHelper.GetClientAuthTokenHex();
                if (!string.IsNullOrEmpty(token)) tas.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, string.Format("!linksteam {0}", token), false);
            }
            foreach (User u in tas.ExistingUsers.Values.ToList().Where(x => x.SteamID != null && friends.Contains(x.SteamID.Value))) AddFriend(u.Name);
            steamVoice.Init(SteamID);
        }
    }
}