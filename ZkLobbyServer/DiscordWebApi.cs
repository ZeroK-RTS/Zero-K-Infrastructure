using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using ZkData;
using System.Linq;

namespace ZkLobbyServer
{
    public class DiscordWebApi
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private ConcurrentDictionary<int, string> userStates = new ConcurrentDictionary<int, string>();
        private ConcurrentDictionary<string, int> userIds = new ConcurrentDictionary<string, int>();

        public DiscordWebApi(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        public string GetAuthenticationURL(int accountId)
        {
            return string.Format("https://discordapp.com/api/oauth2/authorize?client_id={0}&redirect_uri={1}&response_type=code&scope=identify&state={2}", clientId, Uri.EscapeDataString(GetRedirectURL()), Uri.EscapeDataString(GetAnonymousId(accountId)));
        }

        public string GetRedirectURL()
        {
            return GlobalConst.BaseSiteUrl + "/Home/DiscordAuth";
        }

        public string GetAnonymousId(int accountId)
        {
            Guid g = Guid.NewGuid();
            var state = Convert.ToBase64String(g.ToByteArray());
            state = userStates.GetOrAdd(accountId, state);
            userIds.TryAdd(state, accountId);
            return state;
        }

        public async Task LinkAccount(string state, string code)
        {
            try
            {
                int accountId;
                Trace.TraceInformation("Linking discord id...");
                if (!userIds.TryGetValue(state, out accountId))
                {
                    Trace.TraceWarning("Invalid state " + state);
                    return;
                }

                var request = new HttpRequestMessage(HttpMethod.Post, "https://discordapp.com/api/oauth2/token");
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                    { "client_id", GlobalConst.ZeroKDiscordID },
                    { "client_secret", new Secrets().GetDiscordClientSecret() },
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", GetRedirectURL() },
                    { "scope", "identify" },
                });

                var response = await new HttpClient().SendAsync(request);
                response.EnsureSuccessStatusCode();

                Trace.TraceInformation("Sent discord identify request...");
                var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
                var token = payload.Value<string>("access_token");

                var discord = new DiscordSocketClient();
                Trace.TraceInformation("Starting discord client...");
                await discord.StartAsync();
                Trace.TraceInformation("Logging in discord client...");
                await discord.LoginAsync(TokenType.Bearer, token);
                var discordId = discord.CurrentUser.Id;
                Trace.TraceInformation("Got discord id " + discordId);
                using (var db = new ZkDataContext())
                {
                    var existing = db.Accounts.FirstOrDefault(x => x.DiscordID == discordId);
                    if (existing != null)
                    {
                        Trace.TraceInformation("Unlinking discord for Account " + existing.Name);
                        existing.DiscordID = (decimal?)null;
                        db.SaveChanges();
                    }
                    Trace.TraceInformation("Linking discord id " + discordId + " to Account " + existing.Name);
                    db.Accounts.FirstOrDefault(x => x.AccountID == accountId).DiscordID = discordId;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error linking discord ID " + ex);
            }
        }
    }

}