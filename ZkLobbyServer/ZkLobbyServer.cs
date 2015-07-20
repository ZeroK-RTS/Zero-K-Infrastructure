using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LobbyClient;
using MaxMind.Db;
using MaxMind.GeoIP2;
using ZkData;

namespace ZkLobbyServer
{
    public class ZkLobbyServer
    {
        public int BattleCounter;
        public ConcurrentDictionary<int, Battle> Battles = new ConcurrentDictionary<int, Battle>();
        public int ClientCounter;
        public ConcurrentDictionary<string, ConnectedUser> ConnectedUsers = new ConcurrentDictionary<string, ConnectedUser>();
        public string Engine { get; set; }
        public string Game { get; set; }

        public LoginChecker LoginChecker;
        public OfflineMessageHandler OfflineMessageHandler = new OfflineMessageHandler();
        public ConcurrentDictionary<string, Channel> Rooms = new ConcurrentDictionary<string, Channel>();
        public CommandJsonSerializer Serializer = new CommandJsonSerializer();
        public SteamWebApi SteamWebApi;
        ChatRelay chatRelay;

        public EventHandler<Say> Said = delegate { };

        public string Version { get; private set; }


        public ZkLobbyServer(string geoIPpath)
        {
            var entry = Assembly.GetExecutingAssembly();
            Version = entry.GetName().Version.ToString();
            Engine = GlobalConst.DefaultEngineOverride;
            Game = "zk:stable";
            LoginChecker = new LoginChecker(this, geoIPpath);
            SteamWebApi = new SteamWebApi(GlobalConst.SteamAppID, new Secrets().GetSteamWebApiKey());
            chatRelay = new ChatRelay(this, new Secrets().GetNightwatchPassword(), new List<string>() { "zkdev", "sy", "moddev", "weblobbydev", "ai" });
        }

        public virtual async Task OnSaid(Say say)
        {
            Said(this, say);
        }

        /// <summary>
        /// Broadcast to all targets in paralell
        /// </summary>
        public async Task Broadcast<T>(IEnumerable<ICommandSender> targets, T data)
        {
            //send identical command to many clients
            var line = Serializer.SerializeToLine(data);
            await Task.WhenAll(targets.Where(x => x != null).Select(async (client) => { await client.SendLine(line); }));
        }

        /// <summary>
        /// Broadcasts to all connected users in paralell
        /// </summary>
        public Task Broadcast<T>(IEnumerable<string> targetUsers, T data)
        {
            return Broadcast(targetUsers.Select(x => {
                ConnectedUser cli;
                ConnectedUsers.TryGetValue(x, out cli);
                return cli;
            }), data);
        }

        /// <summary>
        /// Directly say something possibly as another user (skips all checks)
        /// </summary>
        public async Task GhostSay(Say say, int? battleID = null)
        {
            if (say.Time == null) say.Time = DateTime.UtcNow;
            
            
            switch (say.Place) {
                case SayPlace.Channel:
                    Channel channel;
                    if (Rooms.TryGetValue(say.Target, out channel)) await Broadcast(channel.Users.Keys, say);
                    await OfflineMessageHandler.StoreChatHistory(say);
                    break;
                case SayPlace.User:
                    ConnectedUser connectedUser;
                    if (ConnectedUsers.TryGetValue(say.Target, out connectedUser)) await connectedUser.SendCommand(say);
                    else await OfflineMessageHandler.StoreChatHistory(say);
                    break;
                case SayPlace.MessageBox:
                    await Broadcast(ConnectedUsers.Values, say);
                    break;
                case SayPlace.Battle:
                    Battle battle;
                    if (Battles.TryGetValue(battleID.Value, out battle)) await Broadcast(battle.Users.Keys, say);
                    break;
            }

            await OnSaid(say);
        }

        public Task GhostPm(string name, string text)
        {
            return GhostSay(new Say()
            {
                User = GlobalConst.NightwatchName,
                IsEmote = true,
                Place = SayPlace.User,
                Time = DateTime.UtcNow,
                Target = name,
                Text = text
            });
        }

        public async Task SendSiteToLobbyCommand(string user, SiteToLobbyCommand command)
        {
            ConnectedUser conUs;
            if (ConnectedUsers.TryGetValue(user, out conUs)) await conUs.SendCommand(command);
        }

        
        public bool IsLobbyConnected(string user)
        {
            return ConnectedUsers.ContainsKey(user);
        }

        public async Task PublishAccountUpdate(Account acc)
        {
            ConnectedUser conus;
            if (ConnectedUsers.TryGetValue(acc.Name, out conus)) {
                LoginChecker.UpdateUserFromAccount(conus.User, acc);
                await Broadcast(ConnectedUsers.Values, conus.User);
            }
        }
    }
}