using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class ZkLobbyServer
    {
        public int BattleCounter;
        public ConcurrentDictionary<int, ServerBattle> Battles = new ConcurrentDictionary<int, ServerBattle>();
        public ChannelManager ChannelManager;
        private ChatRelay chatRelay;
        public int ClientCounter;
        public ConcurrentDictionary<string, ConnectedUser> ConnectedUsers = new ConcurrentDictionary<string, ConnectedUser>();

        public LoginChecker LoginChecker;
        public OfflineMessageHandler OfflineMessageHandler = new OfflineMessageHandler();
        public ConcurrentDictionary<string, Channel> Rooms = new ConcurrentDictionary<string, Channel>();
        public EventHandler<Say> Said = delegate { };
        public CommandJsonSerializer Serializer = new CommandJsonSerializer();
        public SteamWebApi SteamWebApi;
        public string Engine { get; set; }
        public string Game { get; set; }
        public IPlanetwarsEventCreator PlanetWarsEventCreator { get; private set; }
        public MatchMaker MatchMaker { get; private set; }

        public string Version { get; private set; }


        public ZkLobbyServer(string geoIPpath, IPlanetwarsEventCreator creator)
        {
            PlanetWarsEventCreator = creator;
            var entry = Assembly.GetExecutingAssembly();
            Version = entry.GetName().Version.ToString();
            Engine = GlobalConst.DefaultEngineOverride;
            Game = "zk:stable";
            LoginChecker = new LoginChecker(this, geoIPpath);
            SteamWebApi = new SteamWebApi(GlobalConst.SteamAppID, new Secrets().GetSteamWebApiKey());
            chatRelay = new ChatRelay(this, new Secrets().GetNightwatchPassword(), new List<string>() { "zkdev", "sy", "moddev", "weblobbydev", "ai" });
            ChannelManager = new ChannelManager(this);
            MatchMaker = new MatchMaker(this);
        }

        /// <summary>
        ///     Broadcast to all targets in paralell
        /// </summary>
        public async Task Broadcast<T>(IEnumerable<ICommandSender> targets, T data)
        {
            //send identical command to many clients
            var line = Serializer.SerializeToLine(data);
            await Task.WhenAll(targets.Where(x => x != null).Select(async (client) => { await client.SendLine(line); }));
        }

        /// <summary>
        ///     Broadcasts to all connected users in paralell
        /// </summary>
        public Task Broadcast<T>(IEnumerable<string> targetUsers, T data)
        {
            return Broadcast(targetUsers.Select(x =>
            {
                ConnectedUser cli;
                ConnectedUsers.TryGetValue(x, out cli);
                return cli;
            }),
                data);
        }

        public bool CanChatTo(string origin, string target)
        {
            ConnectedUser usr;
            if (ConnectedUsers.TryGetValue(origin, out usr)) if (usr.IgnoredBy.Contains(target)) return false;
            if (ConnectedUsers.TryGetValue(target, out usr)) if (usr.Ignores.Contains(origin)) return false;
            return true;
        }

        public async Task ForceJoinBattle(string playerName, string battleHost)
        {
            var bat = Battles.Values.FirstOrDefault(x => x.FounderName == battleHost);
            if (bat != null) await ForceJoinBattle(playerName, bat);
        }

        public async Task ForceJoinBattle(string player, Battle bat)
        {
            ConnectedUser connectedUser;
            if (ConnectedUsers.TryGetValue(player, out connectedUser))
            {
                if (connectedUser.MyBattle != null) await connectedUser.Process(new LeaveBattle());
                await connectedUser.Process(new JoinBattle() { BattleID = bat.BattleID, Password = bat.Password });
            }
        }

        public List<Battle> GetPlanetBattles(Planet planet)
        {
            return GetPlanetWarsBattles().Where(x => x.MapName == planet.Resource.InternalName).ToList();
        }

        public List<Battle> GetPlanetWarsBattles()
        {
            return Battles.Values.Where(x => x.Mode == AutohostMode.Planetwars).Cast<Battle>().ToList();
        }

        public Task GhostChanSay(string channelName, string text, bool isEmote = true, bool isRing = false)
        {
            return
                GhostSay(new Say()
                {
                    User = GlobalConst.NightwatchName,
                    IsEmote = isEmote,
                    Place = SayPlace.Channel,
                    Time = DateTime.UtcNow,
                    Target = channelName,
                    Text = text,
                    Ring = isRing
                });
        }

        public Task GhostPm(string name, string text)
        {
            return
                GhostSay(new Say()
                {
                    User = GlobalConst.NightwatchName,
                    IsEmote = true,
                    Place = SayPlace.User,
                    Time = DateTime.UtcNow,
                    Target = name,
                    Text = text
                });
        }

        /// <summary>
        ///     Directly say something possibly as another user (skips all checks)
        /// </summary>
        public async Task GhostSay(Say say, int? battleID = null)
        {
            if (say.Time == null) say.Time = DateTime.UtcNow;

            switch (say.Place)
            {
                case SayPlace.Channel:
                    Channel channel;
                    if (Rooms.TryGetValue(say.Target, out channel)) await Broadcast(channel.Users.Keys.Where(x => CanChatTo(say.User, x)), say);
                    await OfflineMessageHandler.StoreChatHistory(say);
                    break;
                case SayPlace.User:
                    ConnectedUser connectedUser;
                    if (ConnectedUsers.TryGetValue(say.Target, out connectedUser) && CanChatTo(say.User, say.Target)) await connectedUser.SendCommand(say);
                    else await OfflineMessageHandler.StoreChatHistory(say);
                    break;
                case SayPlace.MessageBox:
                    await Broadcast(ConnectedUsers.Values, say);
                    break;
                case SayPlace.Battle:
                    ServerBattle battle;
                    if (Battles.TryGetValue(battleID.Value, out battle)) await Broadcast(battle.Users.Keys.Where(x => CanChatTo(say.User, x)), say);
                    break;
                case SayPlace.BattlePrivate:
                    ConnectedUser originUser;
                    if (ConnectedUsers.TryGetValue(say.Target, out originUser) && CanChatTo(say.User, say.Target)) await originUser.SendCommand(say);
                    break;
            }

            await OnSaid(say);
        }


        public bool IsLobbyConnected(string user)
        {
            return ConnectedUsers.ContainsKey(user);
        }

        public void KickFromServer(string kickerName, string kickeeName, string reason)
        {
            ConnectedUser conus;
            if (ConnectedUsers.TryGetValue(kickeeName, out conus))
            {
                conus.Respond(string.Format("You were kicked for: {0}", reason));
                conus.RequestCloseAll();
            }
        }

        /// <summary>
        ///     Mark all users as disconnected, fixes chat history repeat
        /// </summary>
        public void Shutdown()
        {
            Broadcast(ConnectedUsers.Values,
                new Say()
                {
                    User = GlobalConst.NightwatchName,
                    Text = "Zero-K server restarted for upgrade, be back soon",
                    Place = SayPlace.MessageBox,
                });

            var db = new ZkDataContext();
            foreach (var u in ConnectedUsers.Values)
            {
                if (u != null && u.IsLoggedIn)
                {
                    var acc = db.Accounts.Find(u.User.AccountID);
                    acc.LastLogout = DateTime.UtcNow;
                }
            }
            db.SaveChanges();

            
            // close all existing client connections
            foreach (var usr in ConnectedUsers.Values) if (usr != null) foreach (var con in usr.Connections.Keys) con?.RequestClose();


            foreach (var bat in Battles.Values) if (bat !=null && bat.spring.IsRunning) bat.spring.ExitGame();
        }

        public virtual async Task OnSaid(Say say)
        {
            Said(this, say);
        }

        public async Task PublishAccountUpdate(Account acc)
        {
            ConnectedUser conus;
            if (ConnectedUsers.TryGetValue(acc.Name, out conus))
            {
                LoginChecker.UpdateUserFromAccount(conus.User, acc);
                await Broadcast(ConnectedUsers.Values, conus.User);
            }
        }

        public async Task RemoveBattle(Battle battle)
        {
            foreach (var u in battle.Users.Keys)
            {
                ConnectedUser connectedUser;
                if (ConnectedUsers.TryGetValue(u, out connectedUser)) connectedUser.MyBattle = null;
                await Broadcast(ConnectedUsers.Values, new LeftBattle() { BattleID = battle.BattleID, User = u });
            }
            ServerBattle bat;
            if (Battles.TryRemove(battle.BattleID, out bat)) bat.Dispose();
            await Broadcast(ConnectedUsers.Values, new BattleRemoved() { BattleID = battle.BattleID });
        }


        public async Task SendSiteToLobbyCommand(string user, SiteToLobbyCommand command)
        {
            ConnectedUser conUs;
            if (ConnectedUsers.TryGetValue(user, out conUs)) await conUs.SendCommand(command);
        }

        public async Task SetTopic(string channel, string topic, string author)
        {
            // todo persist in db
            Channel chan;
            if (Rooms.TryGetValue(channel, out chan))
            {
                chan.Topic.Text = topic;
                chan.Topic.SetDate = DateTime.UtcNow;
                chan.Topic.SetBy = author;
                await Broadcast(chan.Users.Keys, new ChangeTopic() { ChannelName = chan.Name, Topic = chan.Topic });
            }
        }
    }
}