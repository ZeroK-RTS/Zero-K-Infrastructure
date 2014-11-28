using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Timers;
using PlasmaShared.ContentService;
using ServiceStack.Text;
using Steamworks;
using ZkData;
using Timer = System.Threading.Timer;

namespace PlasmaShared
{
    public class SteamInterface : IDisposable
    {
        int tickCounter;
        public bool IsOnline { get; private set; }
        Timer timer;

        string webApiKey;
        int steamAppID;

        public event Action SteamOnline = () => { };
        public event Action SteamOffline = () => { };


        public SteamInterface(int appID, string webApiKey = null)
        {
            steamAppID = appID;
            this.webApiKey = webApiKey;
        }

        public void ConnectToSteam()
        {
            TimerOnElapsed(this);
            timer = new Timer(TimerOnElapsed, null, 100, 100);
        }


        void TimerOnElapsed(object sender)
        {
            try
            {
                if (tickCounter % 600 == 0)
                {
                    if (!IsOnline)
                    {
                        if (SteamAPI.Init() && SteamAPI.IsSteamRunning())
                        {
                            IsOnline = true;
                            SteamOnline();
                        }
                        else
                        {
                            IsOnline = false;
                            SteamOffline();
                        }
                    }
                }
                if (IsOnline) SteamAPI.RunCallbacks();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }

            tickCounter++;
        }

        public byte[] GetClientAuthToken()
        {
            var buf = new byte[256];
            uint ticketSize;
            SteamUser.GetAuthSessionTicket(buf, buf.Length, out ticketSize);
            var truncArray = new byte[ticketSize];
            Array.Copy(buf, truncArray, truncArray.Length);
            return truncArray;
        }

        public ulong GetSteamID()
        {
            return SteamUser.GetSteamID().m_SteamID;
        }

        public void SetRichPresence(string status, string myLobbyUserName, Dictionary<string, string> extras = null)
        {
            SteamFriends.SetRichPresence("status", status);
            SteamFriends.SetRichPresence("connect", string.Format("Zero-K.exe spring://@join_user:{0}", myLobbyUserName));
            if (extras != null)
            {
                foreach (var kvp in extras) SteamFriends.SetRichPresence(kvp.Key, kvp.Value);
            }
        }


        public string GetMyName()
        {
            return SteamFriends.GetPersonaName();
        }



        public void AdvertiseGame(string ip, ushort port)
        {
            var ipint = (uint)IPAddress.NetworkToHostOrder((int)IPAddress.Parse(ip).Address);
            SteamUser.AdvertiseGame(SteamUser.GetSteamID(), ipint, port);
        }

        public List<ulong> GetFriends()
        {
            var ret = new List<ulong>();
            var cnt = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < cnt; i++)
            {
                ret.Add(SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate).m_SteamID);
            }
            return ret;
        }


        public string GetClientAuthTokenHex()
        {
            return GetClientAuthToken().ToHex();
        }


        public PlayerInfo WebGetPlayerInfo(ulong steamID)
        {
            var wc = new WebClient();
            var ret =
                wc.DownloadString(
                    string.Format("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}", webApiKey, steamID));

            var response = JsonSerializer.DeserializeFromString<PlayerSummariesResposne>(ret);
            return response.response.players.FirstOrDefault();
        }

        protected class PlayerSummariesResposne
        {
            public Response response { get; set; }

            public class Response
            {
                public List<PlayerInfo> players { get; set; }
            }
        }

        public class PlayerInfo
        {
            public long steamid { get; set; }
            public string personaname { get; set; }
            public long lastlogoff { get; set; }
            public string profileurl { get; set; }
            public string avatar { get; set; }
            public string avatarmedium { get; set; }
            public string avatarfull { get; set; }
            public long primaryclanid { get; set; }
        }

        public ulong WebValidateAuthToken(string hexToken)
        {
            var wc = new WebClient();
            var ret =
                wc.DownloadString(string.Format(
                    "http://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/V0001?key={0}&appid={1}&ticket={2}",
                    webApiKey,
                    steamAppID,
                    hexToken));
            var response = JsonSerializer.DeserializeFromString<AuthenticateUserTicketResponse>(ret);

            return response.response.@params.steamid;
        }


        protected class AuthenticateUserTicketResponse
        {
            public Response response { get; set; }
            public class Response
            {
                public Params @params { get; set; }
                public class Params
                {
                    public string result { get; set; }
                    public ulong steamid { get; set; }
                    public ulong ownersteamid { get; set; }
                }
            }
        }

        public void Dispose()
        {
            if (timer != null) timer.Dispose();
            if (IsOnline) SteamAPI.Shutdown();
        }

        public void StartVoiceRecording()
        {
            SteamUser.StartVoiceRecording();
        }

        public bool GetVoice(bool wantCompressed, byte[] compressedBuffer,uint compressedBufferSize, out uint compressedBufferWritten, bool wantUncompressed, byte[] uncompressedBuffer, uint uncompressedBufferSize, out uint uncompressedBufferWritten, uint sampleRate)
        {
            if (
                SteamUser.GetVoice(wantCompressed,
                    compressedBuffer,
                    compressedBufferSize,
                    out compressedBufferWritten,
                    wantUncompressed,
                    uncompressedBuffer,
                    uncompressedBufferSize,
                    out uncompressedBufferWritten,
                    sampleRate) == EVoiceResult.k_EVoiceResultOK) return true;
            else return false;

        }

        public bool DecompressVoice(byte[] compressed, uint cbCompressed, byte[] dest, uint cbDest, out uint written, uint sampleRate)
        {
            return SteamUser.DecompressVoice(compressed, cbCompressed, dest, cbDest, out written, sampleRate) == EVoiceResult.k_EVoiceResultOK;
        }
    }
}

//public class 

/*new Thread(() =>
                {
                    if (SteamAPI.Init())
                    {
                        byte[] compressedBuffer = new byte[8000];

                        var sid = SteamUser.GetSteamID();
                        uint compressedBufferWritten;
                        uint uncompressedBufferWritten;
                        SteamUser.StartVoiceRecording();
                        //SteamFriends.ActivateGameOverlayToUser("steamid", SteamUser.GetSteamID());
                        while (true)
                        {
                            var ret = SteamUser.GetVoice(true, compressedBuffer, 8000, out compressedBufferWritten, false, null, 0, out uncompressedBufferWritten, 0);
                            Thread.Sleep(50);
                            if (ret != EVoiceResult.k_EVoiceResultNoData) {}

                        }
                    }
                }).Start();*/

/*if (SteamAPI.Init())
{
    var needPaint = Callback<HTML_NeedsPaint_t>.Create(((t) =>
    {
        var toRender = t.pBGRA;
        // lockbits, render to some surface/control

    }));

    var ready = CallResult<HTML_BrowserReady_t>.Create((t,wantCompressed) =>
    {
        var browser = t.unBrowserHandle;
        SteamHTMLSurface.SetSize(browser,800,600);
        SteamHTMLSurface.LoadURL(browser, "http://www.google.com/", null);
    });

    SteamHTMLSurface.Init();
    var handle = SteamHTMLSurface.CreateBrowser(null, null);
    ready.Set(handle);

    while (true)
    {
        SteamAPI.RunCallbacks();
        Thread.Sleep(50);
    }

}*/


/*
 * ISteamUserStats
 * 
name: "SetUserStatsForGame",
version: 1,
httpmethod: "POST",
parameters: [
{
name: "key",
type: "string",
optional: false,
description: "access key"
},
{
name: "steamid",
type: "uint64",
optional: false,
description: "SteamID of user"
},
{
name: "appid",
type: "uint32",
optional: false,
description: "appid of game"
},
{
name: "count",
type: "uint32",
optional: false,
description: "Number of stats and achievements to set a value for (name/value param pairs)"
},
{
name: "name[0]",
type: "string",
optional: false,
description: "Name of stat or achievement to set"
},
{
name: "value[0]",
type: "uint32",
optional: false,
description: "Value to set"
}
]*/
