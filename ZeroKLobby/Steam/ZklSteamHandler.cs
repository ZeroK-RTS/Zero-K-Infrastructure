using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LobbyClient;
using NAudio.Wave;
using PlasmaShared;
using Steamworks;
using ZeroKLobby.Steam;
using ZkData;

namespace ZeroKLobby
{
    public class ZklSteamHandler:IDisposable
    {
        TasClient tas;
        public SteamClientHelper SteamHelper { get; private set; }

        public string SteamName { get; private set; }
        public ulong SteamID { get; private set; }

        List<ulong> friends = new List<ulong>();

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
                if (tas.IsLoggedIn && tas.MyUser!=null && tas.MyUser.EffectiveElo != 0) OnLoggedToBothSteamAndTas();

                var na = new DirectSoundOut();
                var prov = new BufferedWaveProvider(new WaveFormat(44100,1));
                
                //prov.BufferDuration = TimeSpan.FromMilliseconds(1000);

                
                na.Init(prov);
                na.Play();

                new Thread(() =>
                {
                    SteamUser.StartVoiceRecording();
                    var buf = new byte[65535];
                    var dest = new byte[65535];
                    //SteamFriends.ActivateGameOverlayToUser("steamid", SteamUser.GetSteamID());
                    while (true)
                    {
                        uint cbs;
                        uint ubs;
                        Thread.Sleep(100);
                        bool ret1;
                        if (
                            SteamUser.GetVoice(true,
                                buf,
                                (uint)buf.Length,
                                out cbs,
                                false,
                                null,
                                0,
                                out ubs,
                                44100) == EVoiceResult.k_EVoiceResultOK) ret1 = true;
                        else ret1 = false;
                        var ret = ret1;
                        if (ret)
                        {
                            uint writ;
                            bool temp = SteamUser.DecompressVoice(buf, cbs, dest, (uint)dest.Length, out writ, 44100) == EVoiceResult.k_EVoiceResultOK;
                            prov.AddSamples(dest,0,(int)writ);
                        }
                        
                    }
                    
                }).Start();
            };

            tas.MyExtensionsChanged += (sender, args) => { if (SteamHelper.IsOnline && SteamID != 0) OnLoggedToBothSteamAndTas(); };
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
            if (tas.MyUser.SteamID == null)
            {
                var token = SteamHelper.GetClientAuthTokenHex();
                if (!string.IsNullOrEmpty(token)) tas.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, string.Format("!linksteam {0}", token), false);
                else
                {
                    // TODO steam running but not "purchased" -> notify user to register in steam

                }
            }
            foreach (var u in tas.ExistingUsers.Values.ToList().Where(x => x.SteamID != null && friends.Contains(x.SteamID.Value)))
            {
                AddFriend(u.Name);
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

        public void Dispose()
        {
            if (SteamHelper != null)
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
}
