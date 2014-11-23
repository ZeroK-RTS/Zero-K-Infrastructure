using System;
using System.Collections.Generic;
using System.Net;
using System.Timers;
using ServiceStack.Text;
using Steamworks;
using ZkData;

namespace PlasmaShared
{
    public class Steam
    {
        static Steam()
        {
        }

        bool isOnline;
        Timer timer = new Timer();

        string webApiKey;
        int steamAppID;
        public Steam(int appID, string webApiKey = null)
        {
            steamAppID = appID;
            if (SteamAPI.Init() && SteamAPI.IsSteamRunning())
            {
                isOnline = true;
            }

            timer.Interval = 60000;
            timer.AutoReset = true;
            timer.Elapsed += TimerOnElapsed;
            timer.Start();
            this.webApiKey = webApiKey;
        }

        void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (!isOnline)
            {
                if (SteamAPI.Init() && SteamAPI.IsSteamRunning()) isOnline = true;
                else isOnline = false;
            }
        }

        public byte[] GetClientAuthToken()
        {
            var buf = new byte[256];
            uint ticketSize;
            SteamUser.GetAuthSessionTicket(buf, buf.Length, out ticketSize);
            var truncArray = new byte[ticketSize];
            Array.Copy(buf , truncArray , truncArray.Length);
            return truncArray;
        }

        public ulong GetSteamID()
        {
            return SteamUser.GetSteamID().m_SteamID;
        }

        public void SetRichPresence(string status, string myLobbyUserName, Dictionary<string,string>  extras = null)
        {
            SteamFriends.SetRichPresence("status", status);
            SteamFriends.SetRichPresence("connect", string.Format("Zero-K.exe spring://@join_user:{0}", myLobbyUserName));
            if (extras != null)
            {
                foreach (var kvp in extras) SteamFriends.SetRichPresence(kvp.Key, kvp.Value);
            }
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
	}
}

        //public class 

        /*new Thread(() =>
                        {
                            if (SteamAPI.Init())
                            {
                                byte[] buf = new byte[8000];

                                var sid = SteamUser.GetSteamID();
                                uint writ;
                                uint ucb;
                                SteamUser.StartVoiceRecording();
                                //SteamFriends.ActivateGameOverlayToUser("steamid", SteamUser.GetSteamID());
                                while (true)
                                {
                                    var ret = SteamUser.GetVoice(true, buf, 8000, out writ, false, null, 0, out ucb, 0);
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

            var ready = CallResult<HTML_BrowserReady_t>.Create((t,b) =>
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
    