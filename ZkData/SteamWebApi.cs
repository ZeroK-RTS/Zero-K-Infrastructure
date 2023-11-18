using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZkData
{
    public class SteamWebApi
    {
        private int steamAppID;
        private string webApiKey;
        const string lichoSteamId = "76561197962341674";

        public SteamWebApi():this(GlobalConst.SteamAppID, new Secrets().GetSteamWebApiKey()) { }

        public SteamWebApi(int steamAppId, string webApiKey)
        {
            steamAppID = steamAppId;
            this.webApiKey = webApiKey;
        }


        private static T Retry<T>(Func<T> func)
        {
            int tries = 2;

            retry:
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                tries--;
                if (tries > 0) goto retry;
                throw;
            }
        }

        public List<AppBuild> GetAppBuilds()
        {
            using (var wc = new WebClient())
            {
                var ret = wc.DownloadString($"https://api.steampowered.com/ISteamApps/GetAppBuilds/v001/?appid={steamAppID}&key={webApiKey}");
                var response = JsonConvert.DeserializeObject<GetAppBuildsResponse>(ret);
                return Enumerable.OrderByDescending<AppBuild, ulong>(response.response.builds.Values, x => x.BuildID).ToList();
            }
        }

        public void SetAppBuildLive(ulong buildid, string branch = "public")
        {
            var wc = new WebClient();
            var nvc = new NameValueCollection();
            nvc["key"] = webApiKey;
            nvc["appid"] = steamAppID.ToString();
            nvc["buildid"] = buildid.ToString();
            nvc["betakey"] = branch;
            nvc["steamid"] = lichoSteamId;

            var response = Encoding.UTF8.GetString(wc.UploadValues($"https://partner.steam-api.com/ISteamApps/SetAppBuildLive/v2/", nvc));
        }


        public bool CheckAppOwnership(ulong steamID, ulong appID)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    var ret = wc.DownloadString(string.Format(
                        "https://partner.steam-api.com/ISteamUser/CheckAppOwnership/v2/?key={0}&steamid={1}&appid={2}",
                        webApiKey,
                        steamID,
                        appID));

                    var response = JsonConvert.DeserializeObject<CheckAppOwnershipResponse>(ret);
                    return response?.appownership?.ownsapp == true && response?.appownership?.permanent == true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error checking app ownership {0} {1}: {2}", steamID, appID, ex);
            }

            return false;
        }


        public async Task<PlayerInfo> VerifyAndGetAccountInformation(string token)
        {
            if (!string.IsNullOrEmpty(token))
                try
                {
                    var steamID = WebValidateAuthToken(token);
                    var info = WebGetPlayerInfo(steamID);
                    info.steamid = steamID;
                    return info;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error linking steam account: {0}", ex);
                }
            return null;
        }

        public PlayerInfo WebGetPlayerInfo(ulong steamID)
        {
            using (var wc = new WebClient())
            {
                var ret = wc.DownloadString(string.Format("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}",
                    webApiKey,
                    steamID));

                var response = JsonConvert.DeserializeObject<PlayerSummariesResposne>(ret);
                return Enumerable.FirstOrDefault<PlayerInfo>(response.response.players);
            }
        }

        public ulong WebValidateAuthToken(string hexToken)
        {
            return Retry(() =>
            {
                using (var wc = new WebClient())
                {
                    var ret = wc.DownloadString(string.Format(
                        "https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/V0001?key={0}&appid={1}&ticket={2}",
                        webApiKey,
                        steamAppID,
                        hexToken));

                    var response = JsonConvert.DeserializeObject<AuthenticateUserTicketResponse>(ret);

                    return response.response.@params.steamid;
                }
            });
        }

        public class AppBuild
        {
            public string Description;
            public ulong AccountIDCreator { get; set; }
            public ulong BuildID { get; set; }
            public ulong CreationTime { get; set; }
        }

        public class AuthenticateUserTicketResponse
        {
            public Response response { get; set; }

            public class Response
            {
                public Params @params { get; set; }

                public class Params
                {
                    public ulong ownersteamid { get; set; }
                    public string result { get; set; }
                    public ulong steamid { get; set; }
                }
            }
        }

        public class GetAppBuildsResponse
        {
            public Response response { get; set; }

            public class Response
            {
                public Dictionary<ulong, AppBuild> builds { get; set; } = new Dictionary<ulong, AppBuild>();
            }
        }

        public class PlayerInfo
        {
            public string avatar { get; set; }
            public string avatarfull { get; set; }
            public string avatarmedium { get; set; }
            public long lastlogoff { get; set; }
            public string personaname { get; set; }
            public long primaryclanid { get; set; }
            public string profileurl { get; set; }
            public ulong steamid { get; set; }
        }


        public class CheckAppOwnershipResponse
        {
            public AppOwnership appownership;
        }

        public class AppOwnership
        {
            public bool ownsapp;
            public bool permanent;
            public string timestamp;
            public ulong ownersteamid;
            public bool sitelicense;
        }


        public class PlayerSummariesResposne
        {
            public Response response { get; set; }

            public class Response
            {
                public List<PlayerInfo> players { get; set; }
            }
        }
    }
}

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
