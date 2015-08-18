using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using Steamworks;
using ZeroKLobby.Steam;
using ZkData;

namespace ZeroKLobby
{
    public class ZklSteamHandler : IDisposable
    {
        List<ulong> friends = new List<ulong>();
        readonly TasClient tas;
        public SteamClientHelper SteamHelper { get; private set; }
        public SteamVoiceSystem Voice = new SteamVoiceSystem();

        public ulong SteamID { get; private set; }
        public string SteamName { get; private set; }

        public ZklSteamHandler(TasClient tas)
        {
            this.tas = tas;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
				if (Environment.Is64BitProcess)
				{
					EmbeddedResourceExtractor.ExtractFile ("ZeroKLobby.NativeLibs.libCSteamworks-x86_64.so", "libCSteamworks.so");
					EmbeddedResourceExtractor.ExtractFile ("ZeroKLobby.NativeLibs.libsteam_api-x86_64.so", "libsteam_api.so");
				}
				else
				{
					EmbeddedResourceExtractor.ExtractFile ("ZeroKLobby.NativeLibs.libCSteamworks.so", "libCSteamworks.so");
					EmbeddedResourceExtractor.ExtractFile ("ZeroKLobby.NativeLibs.libsteam_api.so", "libsteam_api.so");
				}
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


            tas.MyUserStatusChanged += (sender, args) => { if (SteamHelper.IsOnline && SteamID != 0) OnLoggedToBothSteamAndTas(); };
            tas.UserStatusChanged += (sender, args) =>
            {
                if (args?.New?.SteamID != null && args?.Old?.SteamID != args?.New?.SteamID && args?.New?.SteamID !=0) {
                    Voice.AddListenerSteamID(args.New.SteamID.Value); // todo only for battle in future
                    if (friends.Contains(args.New.SteamID.Value)) AddFriend(args.New.Name);
                }
            };

            tas.UserRemoved += (sender, args) =>
            {
                User us;
                if (tas.ExistingUsers.TryGetValue(args.Name, out us) && us.SteamID.HasValue)
                {
                    Voice.RemoveListenerSteamID(us.SteamID.Value);
                }
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
                if (!string.IsNullOrEmpty(token)) tas.LinkSteam(token);
            }
            foreach (User u in tas.ExistingUsers.Values.ToList().Where(x => x.SteamID != null && friends.Contains(x.SteamID.Value))) AddFriend(u.Name);
            if (Program.Conf.EnableVoiceChat && Environment.OSVersion.Platform != PlatformID.Unix)
            {
                Voice.Init(SteamID);
            }
        }
    }
}