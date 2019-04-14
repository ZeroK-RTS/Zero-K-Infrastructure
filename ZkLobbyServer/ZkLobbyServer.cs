using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKWeb;
using ZkData;
using Ratings;

namespace ZkLobbyServer
{
    public class ZkLobbyServer
    {
        public ConcurrentDictionary<int, ServerBattle> Battles = new ConcurrentDictionary<int, ServerBattle>();
        public ChannelManager ChannelManager;
        public ChatRelay chatRelay;
        public int ClientCounter;
        public ConcurrentDictionary<string, ConnectedUser> ConnectedUsers = new ConcurrentDictionary<string, ConnectedUser>();


        public LoginChecker LoginChecker;
        public OfflineMessageHandler OfflineMessageHandler = new OfflineMessageHandler();
        public ConcurrentDictionary<string, Channel> Channels = new ConcurrentDictionary<string, Channel>();
        public EventHandler<Say> Said = delegate { };
        public CommandJsonSerializer Serializer = new CommandJsonSerializer(Utils.GetAllTypesWithAttribute<MessageAttribute>());
        public SteamWebApi SteamWebApi;

        private ServerTextCommands textCommands;
        public string Engine { get; private set; }
        public string Game { get; private set; }
        public IPlanetwarsEventCreator PlanetWarsEventCreator { get; private set; }
        public MatchMaker MatchMaker { get; private set; }
        
        public string Version { get; private set; }

        public PlasmaDownloader.PlasmaDownloader Downloader { get; private set; }
        public SpringPaths SpringPaths { get; private set; }

        public ConcurrentDictionary<string,int> SessionTokens = new ConcurrentDictionary<string, int>();

        private BattleListUpdater battleListUpdater;

        public PartyManager PartyManager { get; private set; }

        public PlanetWarsMatchMaker PlanetWarsMatchMaker { get; private set; }

        public NewsListManager NewsListManager { get; private set; }
        public LadderListManager LadderListManager { get; private set; }
        public ForumListManager ForumListManager { get; private set; }


        public ZkLobbyServer(string geoIPpath, IPlanetwarsEventCreator creator)
        {
            RatingSystems.Init();
            MapRatings.Init();
            
            PlanetWarsEventCreator = creator;
            var entry = Assembly.GetExecutingAssembly();
            Version = entry.GetName().Version.ToString();
            Engine = MiscVar.DefaultEngine;

            SpringPaths = new SpringPaths(GlobalConst.SpringieDataDir, false, false);
            Downloader = new PlasmaDownloader.PlasmaDownloader(null, SpringPaths);
            Downloader.GetResource(DownloadType.ENGINE, MiscVar.DefaultEngine);
            Downloader.PackageDownloader.DoMasterRefresh();

            Game = MiscVar.LastRegisteredZkVersion;

            LoginChecker = new LoginChecker(this, geoIPpath);
            SteamWebApi = new SteamWebApi(GlobalConst.SteamAppID, new Secrets().GetSteamWebApiKey());
            chatRelay = new ChatRelay(this, new List<string>() { "zkdev", "sy", "ai", "zk", "zkmap", "springboard", GlobalConst.ModeratorChannel, GlobalConst.CoreChannel, "off-topic", "support","modding" });
            textCommands = new ServerTextCommands(this);
            ChannelManager = new ChannelManager(this);
            MatchMaker = new MatchMaker(this);
            battleListUpdater = new BattleListUpdater(this);
            PartyManager = new PartyManager(this);
            PlanetWarsMatchMaker = new PlanetWarsMatchMaker(this);
            NewsListManager = new NewsListManager(this);
            LadderListManager = new LadderListManager(this);
            ForumListManager = new ForumListManager(this);

            SpawnAutohosts();
            
            RatingSystems.GetRatingSystems().ForEach(x => x.RatingsUpdated += (sender, data) => 
            {
                var db = new ZkDataContext();
                var updatedUsers = ConnectedUsers.Select(c => c.Value.User.AccountID).Intersect(data.affectedPlayers).ToHashSet();
                db.Accounts.Where(acc => updatedUsers.Contains(acc.AccountID)).ForEach(p =>
                {
                    PublishAccountUpdate(p);
                    PublishUserProfileUpdate(p);
                });
            });
        }

        private async Task SpawnAutohosts()
        {
            using (var db = new ZkDataContext())
            {
                foreach (var autohost in db.Autohosts)
                {
                    var battle = new ServerBattle(this, "Autohost");
                    battle.UpdateWith(autohost);
                    await AddBattle(battle);
                }
            }
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
        ///     Broadcast to all connected users in paralell
        /// </summary>
        public async Task Broadcast<T>(T data)
        {
            //send identical command to many clients
            var line = Serializer.SerializeToLine(data);
            await Task.WhenAll(ConnectedUsers.Values.Where(x => x != null).Select(async (client) => { await client.SendLine(line); }));
        }


        /// <summary>
        ///     Broadcasts to all connected users in paralell
        /// </summary>
        public Task Broadcast<T>(IEnumerable<string> targetUsers, T data)
        {
            return Broadcast(targetUsers.Where(x => x != null).Select(x =>
            {
                ConnectedUser cli;
                ConnectedUsers.TryGetValue(x, out cli);
                return cli;
            }),
                data);
        }


        public async Task SendToUser<T>(string name, T data)
        {
            ConnectedUser conus;
            if (ConnectedUsers.TryGetValue(name, out conus)) await conus.SendCommand(data);
        }

        public bool CanChatTo(string origin, string target)
        {
            ConnectedUser usr;
            if (origin == GlobalConst.NightwatchName) return true;
            if (ConnectedUsers.TryGetValue(origin, out usr)) if (usr.IgnoredBy.Contains(target)) return false;
            if (ConnectedUsers.TryGetValue(target, out usr)) if (usr.Ignores.Contains(origin)) return false;
            return true;
        }


        /// <summary>
        /// Mutually syncs user
        /// </summary>
        /// <param name="newUser">one group</param>
        /// <param name="others">second group</param>
        public async Task TwoWaySyncUsers(string newUser, IEnumerable<string> others)
        {
            var uNewUser = ConnectedUsers.Get(newUser);
            if (uNewUser == null) return;
            var uOthers = others.Select(x => ConnectedUsers.Get(x)).Where(x => x != null).ToList();

            var visibleToNew = uOthers.Where(x => CanUserSee(uNewUser, x) && !HasSeen(uNewUser, x));
            var othersWhoSee = uOthers.Where(x => CanUserSee(x, uNewUser) && !HasSeen(x, uNewUser));

            foreach (var other in visibleToNew) await uNewUser.SendCommand(other.User);
            await Broadcast(othersWhoSee, uNewUser.User);
        }

        public async Task SyncUserToAll(ConnectedUser changer)
        {
            await Broadcast(ConnectedUsers.Values.Where(x=>x!=null).Where(x => CanUserSee(x, changer) && !HasSeen(x, changer)), changer.User);
        }


        public bool CanUserSee(string watcher, string watched)
        {
            if (watcher == watched) return true; // can see self

            ConnectedUser uWatcher;
            ConnectedUser uWatched;
            if (!ConnectedUsers.TryGetValue(watcher, out uWatcher) || !ConnectedUsers.TryGetValue(watched, out uWatched)) return false;

            return CanUserSee(uWatcher, uWatched);
        }

        public bool CanUserSee(ConnectedUser uWatcher, ConnectedUser uWatched)
        {
            if (uWatched == null || uWatcher == null) return false;
            if (uWatched.Name == uWatcher.Name) return true;

            // admins always visible
            if (uWatched.User?.IsAdmin == true) return true;

            // friends see each other
            if (uWatcher.FriendNames.Contains(uWatched.Name)) return true;

            // already seen, cannot be unseen
            if (uWatcher.HasSeenUserVersion.ContainsKey(uWatched.Name)) return true;

            // clanmates see each other
            if (uWatcher.User?.Clan != null && uWatcher.User?.Clan == uWatched.User?.Clan) return true;

            // people in same battle see each other
            if (uWatcher.MyBattle != null && uWatcher.MyBattle == uWatched.MyBattle) return true;

            // people in same party see each other
            if (uWatcher.User?.PartyID != null && uWatcher.User.PartyID == uWatched.User?.PartyID) return true;


            // people in same non "zk" channel see each other
            foreach (var chan in Channels.Values.Where(x => x != null))
            {
                if (chan.Users.ContainsKey(uWatcher.Name)) // my channel
                {
                    if (chan.IsDeluge)
                    {
                        if (GlobalConst.DelugeChannelDisplayUsers > 0)
                        {
                            var myEffectiveElo = uWatcher?.User?.EffectiveElo ?? 1200;

                            var channelUsersBySkill = chan.Users.Keys.Select(x => ConnectedUsers.Get(x)).Where(x => x != null)
                                .OrderBy(x => Math.Abs((x.User?.EffectiveMmElo??1200) - myEffectiveElo)).Select(x => x.Name).Take(GlobalConst.DelugeChannelDisplayUsers);

                            if (channelUsersBySkill.Contains(uWatched.Name)) return true;
                        }
                    }
                    else
                    {
                        if (chan.Users.ContainsKey(uWatched.Name)) return true;
                    }
                }
            }
            return false;
        }

        public bool HasSeen(string watcher, string watched)
        {
            ConnectedUser uWatcher;
            ConnectedUser uWatched;
            if (!ConnectedUsers.TryGetValue(watcher, out uWatcher) || !ConnectedUsers.TryGetValue(watched, out uWatched)) return true;
            return HasSeen(uWatcher, uWatched);
        }

        public static bool HasSeen(ConnectedUser uWatcher, ConnectedUser uWatched)
        {
            if (uWatched == null || uWatcher == null) return true;
            int lastSync;
            var newSync = uWatched.User.SyncVersion;
            if (!uWatcher.HasSeenUserVersion.TryGetValue(uWatched.Name, out lastSync) || lastSync != newSync)
            {
                uWatcher.HasSeenUserVersion[uWatched.Name] = newSync;
                return false;
            }
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

        public async Task RequestJoinPlanet(string name, int planetID)
        {
            var conus = ConnectedUsers.Get(name);
            if (conus != null) await conus.SendCommand(new PwRequestJoinPlanet() { PlanetID = planetID });
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

        public async Task UserLogSay(string text)
        {
            Trace.TraceInformation("UserLog: " + text);
            var say = new Say() { Place = SayPlace.Channel, Target = GlobalConst.UserLogChannel, Text = text, User = GlobalConst.NightwatchName, Time = DateTime.UtcNow };
            await GhostSay(say);
        }

        private async Task SyncAndSay(IEnumerable<string> targetNames, Say say)
        {
            var targets = targetNames.Where(x => CanChatTo(say.User, x)).ToList();
            var user = ConnectedUsers.Get(say.User);
            if (user != null) await Broadcast(targets.Where(x => !HasSeen(x, say.User)), user.User); // sync user 
            await Broadcast(targets, say);
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
                    if (Channels.TryGetValue(say.Target, out channel)) await SyncAndSay(channel.Users.Keys, say);
                    OfflineMessageHandler.StoreChatHistoryAsync(say);
                    break;
                case SayPlace.User:
                    ConnectedUser connectedUser;
                    if (ConnectedUsers.TryGetValue(say.Target, out connectedUser)) await SyncAndSay(new List<string>() {say.Target}, say);
                    else OfflineMessageHandler.StoreChatHistoryAsync(say);
                    if (say.User != GlobalConst.NightwatchName && ConnectedUsers.TryGetValue(say.User, out connectedUser)) await connectedUser.SendCommand(say);
                    break;
                case SayPlace.Battle:
                    ServerBattle battle;
                    if (Battles.TryGetValue(battleID ?? 0, out battle))
                    {
                        await SyncAndSay(battle.Users.Keys, say);
                        await battle.ProcessBattleSay(say);
                        OfflineMessageHandler.StoreChatHistoryAsync(say);
                    }
                    break;

                // admin AH sent only:
                case SayPlace.MessageBox:
                    await Broadcast(ConnectedUsers.Values, say);
                    break;
                case SayPlace.BattlePrivate:
                    ConnectedUser targetUser;
                    if (ConnectedUsers.TryGetValue(say.Target, out targetUser)) await targetUser.SendCommand(say);
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


            //foreach (var bat in Battles.Values) if (bat != null && bat.spring.IsRunning) bat.spring.ExitGame();
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
                await SyncUserToAll(conus);

                // join/leave to default channels
                var defaultChannels = ChannelManager.GetDefaultChannels(acc);
                foreach (var chan in Channels)
                {
                    if (chan.Value.Users.ContainsKey(acc.Name) && !ChannelManager.CanJoin(acc, chan.Key)) await conus.Process(new LeaveChannel() { ChannelName = chan.Key });
                    else if (!chan.Value.Users.ContainsKey(acc.Name) && defaultChannels.Contains(acc.Name)) await conus.Process(new JoinChannel() { ChannelName = chan.Key, Password = chan.Value.Password });
                }
            }
        }


        public async Task AddBattle(ServerBattle battle)
        {
            Battles[battle.BattleID] = battle;
            await Broadcast(ConnectedUsers.Keys, new BattleAdded() { Header = battle.GetHeader() });
        }



        public async Task PublishUserProfileUpdate(Account acc)
        {
            ConnectedUser conus;
            if (ConnectedUsers.TryGetValue(acc.Name, out conus))
            {
                await conus.SendCommand(acc.ToUserProfile());
            }
        }

        public async Task PublishUserProfilePlanetwarsPlayers()
        {
            ConnectedUsers.Values.Where(x => x != null && x.IsLoggedIn && !string.IsNullOrEmpty(x.User.Faction)).AsParallel().ForAll(conus =>
            {
                using (var db = new ZkDataContext())
                {
                    var acc = db.Accounts.Find(conus.User.AccountID);
                    if (conus.IsLoggedIn) PublishUserProfileUpdate(acc);
                }
            });
        }



        public async Task RemoveBattle(Battle battle)
        {
            foreach (var u in battle.Users.Keys)
            {
                ConnectedUser connectedUser;
                if (ConnectedUsers.TryGetValue(u, out connectedUser))
                {
                    connectedUser.MyBattle = null;
                    await SyncUserToAll(connectedUser);
                }
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
            Channel chan;
            if (Channels.TryGetValue(channel, out chan))
            {
                chan.Topic.Text = topic;
                chan.Topic.SetDate = DateTime.UtcNow;
                chan.Topic.SetBy = author;
                await Broadcast(chan.Users.Keys, new ChangeTopic() { ChannelName = chan.Name, Topic = chan.Topic });

                try
                {
                    using (var db = new ZkDataContext())
                    {
                        var entry = db.LobbyChannelTopics.FirstOrDefault(x => x.ChannelName == channel);
                        if (entry == null)
                        {
                            entry = new LobbyChannelTopic() { ChannelName = channel };
                            db.LobbyChannelTopics.Add(entry);
                        }
                        entry.Topic = topic;
                        entry.Author = author;
                        entry.SetDate = DateTime.UtcNow;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Failed to set channel topic {0} : {1}",  channel, ex);
                }

            }
        }

        public int GetDiscordUserCount() => chatRelay.DiscordZkUserCount;

        public async Task SetEngine(string engine)
        {
            Engine = engine;
            await Broadcast(new DefaultEngineChanged() { Engine = engine });
        }

        public async Task SetGame(string game)
        {
            Game = game;
            await Broadcast(new DefaultGameChanged() { Game = game });
            await MatchMaker.OnServerGameChanged(game);
            await Task.WhenAll(Battles.Values.Where(x => x.IsDefaultGame).Select(x => x.SwitchGame(game)));
        }

        public async Task OnServerMapsChanged()
        {
            await MatchMaker.OnServerMapsChanged();
        }

        public void RemoveSessionsForAccountID(int accountID)
        {
            foreach (var todel in SessionTokens.Where(x => x.Value == accountID).Select(x => x.Key).ToList())
            {
                int entry;
                SessionTokens.TryRemove(todel, out entry);
            }
        }

    }
}
