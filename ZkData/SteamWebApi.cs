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

        public SteamWebApi():this(GlobalConst.SteamAppID, new Secrets().GetSteamWebApiKey()) { }

        public SteamWebApi(int steamAppId, string webApiKey)
        {
            steamAppID = steamAppId;
            this.webApiKey = webApiKey;
        }

        public List<AppBuild> GetAppBuilds()
        {
            var wc = new WebClient();
            var ret = wc.DownloadString($"https://api.steampowered.com/ISteamApps/GetAppBuilds/v001/?appid={steamAppID}&key={webApiKey}");
            var response = JsonConvert.DeserializeObject<GetAppBuildsResponse>(ret);
            return Enumerable.OrderByDescending<AppBuild, ulong>(response.response.builds.Values, x => x.BuildID).ToList();
        }

        public void SetAppBuildLive(ulong buildid, string branch = "public")
        {
            var wc = new WebClient();
            var nvc = new NameValueCollection();
            nvc["key"] = webApiKey;
            nvc["appid"] = steamAppID.ToString();
            nvc["buildid"] = buildid.ToString();
            nvc["betakey"] = branch;

            var response = Encoding.UTF8.GetString(wc.UploadValues($"https://api.steampowered.com/ISteamApps/SetAppBuildLive/v001/", nvc));
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
            var wc = new WebClient();
            var ret =
                wc.DownloadString(string.Format("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}",
                    webApiKey,
                    steamID));

            var response = JsonConvert.DeserializeObject<PlayerSummariesResposne>(ret);
            return Enumerable.FirstOrDefault<PlayerInfo>(response.response.players);
        }

        public ulong WebValidateAuthToken(string hexToken)
        {
            var wc = new WebClient();
            var ret =
                wc.DownloadString(
                    string.Format("https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/V0001?key={0}&appid={1}&ticket={2}",
                        webApiKey,
                        steamAppID,
                        hexToken));
            var response = JsonConvert.DeserializeObject<AuthenticateUserTicketResponse>(ret);

            return response.response.@params.steamid;
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
