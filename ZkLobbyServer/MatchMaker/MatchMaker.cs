using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public partial class MatchMaker
    {
        private const int TimerSeconds = 30;
        private const int MapModChangePauseSeconds = 30;

        private const int BanSecondsIncrease = 30;
        private const int BanSecondsMax = 300;
        private const int BanReset = 600;


        private struct QueueConfig
        {
            public string Name, Description;
            public Func<Resource, bool> MapSelector;
            public int MaxPartySize, MaxSize, MinSize;
            public double EloCutOffExponent;
            public AutohostMode Mode;
        }

        public class BanInfo
        {
            public DateTime BannedTime;
            public int BanCounter;
            public int BanSeconds;
        }

        private ConcurrentDictionary<string, BanInfo> bannedPlayers = new ConcurrentDictionary<string, BanInfo>();
        private Dictionary<string, int> ingameCounts = new Dictionary<string, int>();

        private List<ProposedBattle> invitationBattles = new List<ProposedBattle>();
        private ConcurrentDictionary<string, PlayerEntry> players = new ConcurrentDictionary<string, PlayerEntry>();
        private List<MatchMakerSetup.Queue> possibleQueues = new List<MatchMakerSetup.Queue>();
        private List<QueueConfig> queueConfigs = new List<QueueConfig>();

        private Dictionary<string, int> queuesCounts = new Dictionary<string, int>();

        private ZkLobbyServer server;


        private object tickLock = new object();
        private Timer timer;
        private int totalQueued;
        private DateTime lastQueueUpdate = DateTime.Now;

        public MatchMaker(ZkLobbyServer server)
        {
            this.server = server;

            Func<Resource, bool> IsTeamsMap = x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.MapIsTeams != false) && (x.TypeID == ResourceType.Map) && x.MapIsSpecial != true;
            Func<Resource, bool> IsCoopMap = x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.TypeID == ResourceType.Map) && x.MapIsSpecial != true;
            Func<Resource, bool> Is1v1Map = x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.TypeID == ResourceType.Map) && x.MapIs1v1 == true && x.MapIsSpecial != true;

            queueConfigs.Add(new QueueConfig()
            {
                Name = "Teams",
                Description = "Play 2v2 to 4v4 with players of similar skill.",
                MinSize = 4,
                MaxSize = 8,
                MaxPartySize = 4,
                EloCutOffExponent = 0.96,
                Mode = AutohostMode.Teams,
                MapSelector = IsTeamsMap,
            });

            queueConfigs.Add(new QueueConfig()
            {
                Name = "Coop",
                Description = "Play together, against AI or chickens",
                MinSize = 2,
                MaxSize = 5,
                MaxPartySize = 5,
                EloCutOffExponent = 0,
                Mode = AutohostMode.GameChickens,
                MapSelector = IsCoopMap,
            });

            queueConfigs.Add(new QueueConfig()
            {
                Name = "1v1",
                Description = "1v1 with opponent of similar skill",
                MinSize = 2,
                MaxSize = 2,
                EloCutOffExponent = 0.975,
                MaxPartySize = 1,
                Mode = AutohostMode.Game1v1,
                MapSelector = Is1v1Map,
            });

            UpdateQueues();

            timer = new Timer(TimerSeconds * 1000);
            timer.AutoReset = true;
            timer.Elapsed += TimerTick;
            timer.Start();

            queuesCounts = CountQueuedPeople(players.Values);
            ingameCounts = CountIngamePeople();
        }

        private void UpdateQueues()
        {
            lastQueueUpdate = DateTime.Now;
            using (var db = new ZkDataContext())
            {
                var oldQueues = possibleQueues;
                possibleQueues = queueConfigs.Select(x =>
                {
                    MatchMakerSetup.Queue queue = new MatchMakerSetup.Queue();
                    if (oldQueues.Exists(y => y.Name == x.Name))
                    {
                        queue = oldQueues.Find(y => y.Name == x.Name);
                    }
                    var oldmaps = queue.Maps;
                    queue.Name = x.Name;
                    queue.Description = x.Description;
                    queue.MinSize = x.MinSize;
                    queue.MaxSize = x.MaxSize;
                    queue.MaxPartySize = x.MaxPartySize;
                    queue.EloCutOffExponent = x.EloCutOffExponent;
                    queue.Game = server.Game;
                    queue.Mode = x.Mode;
                    queue.Maps =
                        db.Resources
                            .Where(x.MapSelector)
                            .Select(y => y.InternalName)
                            .ToList();
                    queue.SafeMaps = queue.Maps.Where(y => oldmaps.Contains(y)).ToList();
                    return queue;
                }).ToList();
            }
        }


        public void AreYouReadyResponse(ConnectedUser user, AreYouReadyResponse response)
        {
            lock (tickLock)
            {
                bool playersUpdated = false;
                bool statusUpdated = false;
                List<PlayerEntry> invitedPeople = null;
                //dont accept AreYouReadyResponse while tick is generating battles
                PlayerEntry entry;
                if (players.TryGetValue(user.Name, out entry))
                    if (entry.InvitedToPlay)
                    {
                        if (response.Ready) entry.LastReadyResponse = true;
                        else
                        {
                            entry.LastReadyResponse = false;
                            playersUpdated = RemoveUser(user.Name);
                        }

                        invitedPeople = players.Values.Where(x => x?.InvitedToPlay == true).ToList();

                        if (invitedPeople.Count <= 1)
                        {
                            foreach (var p in invitedPeople) p.LastReadyResponse = true;
                            // if we are doing tick because too few people, make sure we count remaining people as readied to not ban them 
                            OnTick();
                        }
                        else if (invitedPeople.All(x => x.LastReadyResponse)) OnTick();
                        else
                        {
                            statusUpdated = true;
                        }
                    }

                if (invitedPeople != null && statusUpdated)
                {
                    var readyCounts = CountQueuedPeople(invitedPeople.Where(x => x.LastReadyResponse));

                    var proposedBattles = ProposeBattles(invitedPeople.Where(x => x.LastReadyResponse));

                    Task.WhenAll(invitedPeople.Select(async (p) =>
                    {
                        var invitedBattle = invitationBattles?.FirstOrDefault(x => x.Players.Contains(p));
                        await server.SendToUser(p.Name,
                            new AreYouReadyUpdate()
                            {
                                QueueReadyCounts = readyCounts,
                                ReadyAccepted = p.LastReadyResponse == true,
                                LikelyToPlay = proposedBattles.Any(y => y.Players.Contains(p)),
                                YourBattleSize = invitedBattle?.Size,
                                YourBattleReady = invitedPeople.Count(x => x.LastReadyResponse && (invitedBattle?.Players.Contains(x) == true))
                            });
                    }));
                }

                if (playersUpdated)
                {
                    UpdateAllPlayerStatuses();
                }
            }
        }

        public int GetTotalWaiting() => totalQueued;


        public async Task OnLoginAccepted(ConnectedUser conus)
        {
            await conus.SendCommand(new MatchMakerSetup() { PossibleQueues = possibleQueues });
            await UpdatePlayerStatus(conus.Name);
        }

        public async Task OnServerGameChanged(string game)
        {
            UpdateQueues();
            await server.Broadcast(new MatchMakerSetup() { PossibleQueues = possibleQueues });
        }

        public async Task OnServerMapsChanged()
        {
            UpdateQueues();
            await server.Broadcast(new MatchMakerSetup() { PossibleQueues = possibleQueues });
        }

        public void QueueRequest(ConnectedUser user, MatchMakerQueueRequest cmd)
        {
            lock (tickLock)
            {
                var banTime = BannedSeconds(user.Name);
                if (banTime != null)
                {
                    UpdatePlayerStatus(user.Name);
                    user.Respond($"Please rest and wait for {banTime}s because you refused previous match");
                    return;
                }

                // already invited ignore requests
                PlayerEntry entry;
                if (players.TryGetValue(user.Name, out entry) && entry.InvitedToPlay)
                {
                    UpdatePlayerStatus(user.Name);
                    return;
                }

                var wantedQueueNames = cmd.Queues?.ToList() ?? new List<string>();
                var wantedQueues = possibleQueues.Where(x => wantedQueueNames.Contains(x.Name)).ToList();

                var party = server.PartyManager.GetParty(user.Name);
                if (party != null)
                    wantedQueues = wantedQueues.Where(x => x.MaxSize / 2 >= party.UserNames.Count)
                        .ToList(); // if is in party keep only queues where party fits

                if (wantedQueues.Count == 0) // delete
                {
                    RemoveUser(user.Name, true);
                    return;
                }

                AddOrUpdateUser(user, wantedQueues);
            }
        }

        /// <summary>
        /// Removes user (and his party) from MM queues, doesnt broadcast changes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool RemoveUser(string name)
        {
            lock (tickLock)
            {
                var party = server.PartyManager.GetParty(name);
                var anyRemoved = false;

                if (party != null)
                {
                    foreach (var n in party.UserNames)
                    {
                        if (RemoveSingleUser(n, n == name)) anyRemoved = true;
                    }
                }
                else
                {
                    anyRemoved = RemoveSingleUser(name, true);
                }

                return anyRemoved;
            }
        }

        /// <summary>
        /// Removes user (and his party) from MM queues
        /// </summary>
        /// <param name="name"></param>
        /// <param name="broadcastChanges">should change be broadcasted/statuses updated</param>
        /// <returns></returns>
        public async Task RemoveUser(string name, bool broadcastChanges)
        {
            bool anyRemoved = RemoveUser(name);
            if (broadcastChanges && anyRemoved) await UpdateAllPlayerStatuses();
        }

        public async Task UpdateAllPlayerStatuses()
        {
            ingameCounts = CountIngamePeople();
            queuesCounts = CountQueuedPeople(players.Values);

            await Task.WhenAll(server.ConnectedUsers.Keys.Where(x => x != null).Select(UpdatePlayerStatus));
        }

        private async Task AddOrUpdateUser(ConnectedUser user, List<MatchMakerSetup.Queue> wantedQueues)
        {
            var party = server.PartyManager.GetParty(user.Name);
            if (party != null)
                foreach (var p in party.UserNames)
                {
                    var conUs = server.ConnectedUsers.Get(p);
                    if (conUs != null)
                        players.AddOrUpdate(p,
                            (str) => new PlayerEntry(conUs.User, wantedQueues, party),
                            (str, usr) =>
                            {
                                usr.UpdateTypes(wantedQueues);
                                usr.Party = party;
                                return usr;
                            });
                }
            else
                players.AddOrUpdate(user.Name,
                    (str) => new PlayerEntry(user.User, wantedQueues, null),
                    (str, usr) =>
                    {
                        usr.UpdateTypes(wantedQueues);
                        usr.Party = null;
                        return usr;
                    });


            // if nobody is invited, we can do tick now to speed up things
            bool doUpdates = false;
            lock (tickLock) {//wait for running tick to finish first
                if (invitationBattles?.Any() != true)
                {
                    OnTick();
                }
                else
                {
                    doUpdates = true;
                }
            }
            if (doUpdates) await UpdateAllPlayerStatuses(); // else we just send statuses

        }


        private int? BannedSeconds(string name)
        {
            BanInfo banEntry;
            if (bannedPlayers.TryGetValue(name, out banEntry) && (DateTime.UtcNow.Subtract(banEntry.BannedTime).TotalSeconds < banEntry.BanSeconds)) return (int)(banEntry.BanSeconds - DateTime.UtcNow.Subtract(banEntry.BannedTime).TotalSeconds);

            // remove old
            if (banEntry != null && DateTime.UtcNow.Subtract(banEntry.BannedTime).TotalSeconds > BanReset) bannedPlayers.TryRemove(name, out banEntry);

            return null;
        }

        private Dictionary<string, int> CountIngamePeople()
        {
            var ncounts = possibleQueues.ToDictionary(x => x.Name, x => 0);
            foreach (var bat in server.Battles.Values.OfType<MatchMakerBattle>().Where(x => (x != null) && x.IsMatchMakerBattle && x.IsInGame))
            {
                var plrs = bat.spring?.Context?.LobbyStartContext?.Players.Count(x => !x.IsSpectator) ?? 0;
                if (plrs > 0)
                {
                    var type = bat.Prototype?.QueueType;
                    if (type != null) ncounts[type.Name] += plrs;
                }
            }
            return ncounts;
        }

        private Dictionary<string, int> CountQueuedPeople(IEnumerable<PlayerEntry> sumPlayers)
        {
            int total = 0;
            var ncounts = possibleQueues.ToDictionary(x => x.Name, x => 0);
            foreach (var plr in sumPlayers.Where(x => x != null))
            {
                total++;
                foreach (var jq in plr.QueueTypes) ncounts[jq.Name]++;
            }
            totalQueued = total; // ugly to both return and set class property, refactor for nicer
            return ncounts;
        }

        public Dictionary<string, int> GetQueueCounts() => queuesCounts;

        private async Task StartBattles(List<ProposedBattle> realBattles)
        {
            try
            {
                await UpdateAllPlayerStatuses(); // This can't be run before ResetAndSendMmInvitations because it reads invitationQueue

                foreach (var bat in realBattles) await StartBattle(bat);
            }
            catch (Exception ex)
            {
                Trace.TraceError("MatchMaker error starting battles: {0}", ex);
            }
        }

        private void OnTick()
        {
            List<ProposedBattle> realBattles = new List<ProposedBattle>();
            lock (tickLock)
            {
                try
                {
                    timer.Stop();
                    realBattles = ResolveToRealBattles();
                    
                    ResetAndSendMmInvitations();
                    
                }
                catch (Exception ex)
                {
                    Trace.TraceError("MatchMaker tick error: {0}", ex);
                }
                finally
                {
                    timer.Start();
                }
            }

            //do non critical updates to clients:
            StartBattles(realBattles);
        }

        private static List<ProposedBattle> ProposeBattles(IEnumerable<PlayerEntry> users)
        {
            var proposedBattles = new List<ProposedBattle>();

            var usersByWaitTime = users.OrderBy(x => x.JoinedTime).ToList();
            var remainingPlayers = usersByWaitTime.ToList();

            foreach (var user in usersByWaitTime)
                if (remainingPlayers.Contains(user)) // consider only those not yet assigned
                {
                    var battle = TryToMakeBattle(user, remainingPlayers);
                    if (battle != null)
                    {
                        proposedBattles.Add(battle);
                        remainingPlayers.RemoveAll(x => battle.Players.Contains(x));
                    }
                }

            return proposedBattles;
        }


        private bool RemoveSingleUser(string name, bool banInvited)
        {
            PlayerEntry entry;
            if (players.TryRemove(name, out entry))
            {
                if (entry.InvitedToPlay && banInvited)
                {
                    // was invited but he is gone now (whatever reason), ban!
                    var banEntry = bannedPlayers.GetOrAdd(name, (n) => new BanInfo());
                    banEntry.BannedTime = DateTime.UtcNow;
                    banEntry.BanCounter++;
                    banEntry.BanSeconds = Math.Min(BanSecondsMax, BanSecondsIncrease * banEntry.BanCounter);
                }


                ConnectedUser conUser;
                if (server.ConnectedUsers.TryGetValue(name, out conUser) && (conUser != null)) if (entry?.InvitedToPlay == true)    conUser.SendCommand(new AreYouReadyResult() { AreYouBanned = true, IsBattleStarting = false, });
                return true;
            }
            return false;
        }

        private void ResetAndSendMmInvitations()
        {
            // generate next battles and send inviatation
            invitationBattles = ProposeBattles(players.Values.Where(x => x != null));
            var toInvite = invitationBattles.SelectMany(x => x.Players).ToList();
            foreach (var usr in players.Values.Where(x => x != null))
                if (toInvite.Contains(usr))
                {
                    usr.InvitedToPlay = true;
                    usr.LastReadyResponse = false;
                }
                else
                {
                    usr.InvitedToPlay = false;
                    usr.LastReadyResponse = false;
                }

            server.Broadcast(toInvite.Select(x => x.Name), new AreYouReady() { SecondsRemaining = TimerSeconds });
        }

        private List<ProposedBattle> ResolveToRealBattles()
        {
            var lastMatchedUsers = players.Values.Where(x => x?.InvitedToPlay == true).ToList();

            // force leave those not ready
            foreach (var pl in lastMatchedUsers.Where(x => !x.LastReadyResponse)) RemoveUser(pl.Name);

            var readyUsers = lastMatchedUsers.Where(x => x.LastReadyResponse).ToList();
            var realBattles = ProposeBattles(readyUsers);

            var readyAndStarting = readyUsers.Where(x => realBattles.Any(y => y.Players.Contains(x))).ToList();
            var readyAndFailed = readyUsers.Where(x => !realBattles.Any(y => y.Players.Contains(x))).Select(x => x.Name);

            server.Broadcast(readyAndFailed, new AreYouReadyResult() { IsBattleStarting = false });

            server.Broadcast(readyAndStarting.Select(x => x.Name), new AreYouReadyResult() { IsBattleStarting = true });

            foreach (var usr in readyAndStarting)
            {
                PlayerEntry entry;
                players.TryRemove(usr.Name, out entry);
            }

            return realBattles;
        }

        private string PickMap(MatchMakerSetup.Queue queue)
        {
            Random r = new Random();
            List<string> candidates;
            if (DateTime.Now.Subtract(lastQueueUpdate).TotalSeconds > MapModChangePauseSeconds)
            {
                candidates = queue.Maps;
            }else
            {
                candidates = queue.SafeMaps;
            }
            return candidates.Count == 0 ? "" : candidates[r.Next(candidates.Count)];
        }

        private async Task StartBattle(ProposedBattle bat)
        {
            var battle = new MatchMakerBattle(server, bat, PickMap(bat.QueueType));
            await server.AddBattle(battle);

            // also join in lobby
            foreach (var usr in bat.Players) await server.ForceJoinBattle(usr.Name, battle);

            if (!await battle.StartGame()) await server.RemoveBattle(battle);
        }


        private void TimerTick(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            OnTick();
        }


        private static ProposedBattle TryToMakeBattle(PlayerEntry player, IList<PlayerEntry> otherPlayers)
        {
            var allPlayers = new List<PlayerEntry>();
            allPlayers.AddRange(otherPlayers);
            allPlayers.Add(player);

            var playersByElo =
                otherPlayers.Where(x => x != player)
                    .OrderBy(x => Math.Abs(x.LobbyUser.EffectiveMmElo - player.LobbyUser.EffectiveMmElo))
                    .ThenBy(x => x.JoinedTime)
                    .ToList();

            var testedBattles = player.GenerateWantedBattles(allPlayers);

            foreach (var other in playersByElo)
                foreach (var bat in testedBattles)
                {
                    if (bat.CanBeAdded(other, allPlayers)) bat.AddPlayer(other, allPlayers);
                    if (bat.Players.Count == bat.Size) return bat;
                }
            return null;
        }


        private async Task UpdatePlayerStatus(string name)
        {
            ConnectedUser conus;
            if (server.ConnectedUsers.TryGetValue(name, out conus))
            {
                PlayerEntry entry;
                players.TryGetValue(name, out entry);
                var ret = new MatchMakerStatus()
                {
                    QueueCounts = queuesCounts,
                    IngameCounts = ingameCounts,
                    JoinedQueues = entry?.QueueTypes.Select(x => x.Name).ToList(),
                    CurrentEloWidth = entry?.EloWidth,
                    JoinedTime = entry?.JoinedTime,
                    BannedSeconds = BannedSeconds(name),
                    UserCount = server.ConnectedUsers.Count,
                    UserCountDiscord = server.GetDiscordUserCount()
                };


                // check for instant battle start - only non partied people
                if ((invitationBattles?.Any() != true) && (players.Count > 0) && (server.PartyManager.GetParty(name) == null))
                // nobody invited atm and some in queue
                {
                    ret.InstantStartQueues = new List<string>();
                    // iterate each queue to check all possible instant starts
                    foreach (var queue in possibleQueues)
                    {
                        // get all currently queued players except for self
                        var testPlayers = players.Values.Where(x => (x != null) && (x.Name != name)).ToList();
                        var testSelf = new PlayerEntry(conus.User, new List<MatchMakerSetup.Queue> { queue }, null);
                        testPlayers.Add(testSelf);
                        var testBattles = ProposeBattles(testPlayers);
                        ret.InstantStartQueues.AddRange(testBattles.Where(x => x.Players.Contains(testSelf)).Select(x => x.QueueType.Name).Distinct().ToList());
                    }
                }

                await conus.SendCommand(ret);
            }
        }
    }
}