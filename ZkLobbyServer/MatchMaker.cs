using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using ZkData;
using Timer = System.Timers.Timer;

namespace ZkLobbyServer
{
    public class MatchMaker
    {
        private const int TimerSeconds = 30;

        private const int BanMinutes = 5;

        private ConcurrentDictionary<string, DateTime> bannedPlayers = new ConcurrentDictionary<string, DateTime>();

        private List<ProposedBattle> invitationBattles = new List<ProposedBattle>();
        private ConcurrentDictionary<string, PlayerEntry> players = new ConcurrentDictionary<string, PlayerEntry>();
        private List<MatchMakerSetup.Queue> possibleQueues = new List<MatchMakerSetup.Queue>();

        private Dictionary<string, int> queuesCounts = new Dictionary<string, int>();
        private ZkLobbyServer server;


        private object tickLock = new object();
        private Timer timer;

        public MatchMaker(ZkLobbyServer server)
        {
            this.server = server;
            using (var db = new ZkDataContext())
            {
                possibleQueues.Add(new MatchMakerSetup.Queue()
                {
                    Name = "Teams",
                    Description = "Small teams 2v2 to 4v4 with reasonable skill difference",
                    MaxPartySize = 4,
                    MinSize = 4,
                    MaxSize = 8,
                    Game = server.Game,
                    Mode = AutohostMode.Teams,
                    Maps =
                        db.Resources.Where(
                                x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.MapIsTeams == true) && (x.TypeID == ResourceType.Map))
                            .Select(x => x.InternalName)
                            .ToList()
                });

                possibleQueues.Add(new MatchMakerSetup.Queue()
                {
                    Name = "1v1",
                    Description = "Duels with reasonable skill difference",
                    MaxPartySize = 1,
                    MinSize = 2,
                    MaxSize = 2,
                    Game = server.Game,
                    Maps =
                        db.Resources.Where(
                                x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.MapIs1v1 == true) && (x.TypeID == ResourceType.Map))
                            .Select(x => x.InternalName)
                            .ToList(),
                    Mode = AutohostMode.Game1v1,
                });
            }
            timer = new Timer(TimerSeconds * 1000);
            timer.AutoReset = true;
            timer.Elapsed += TimerTick;
            timer.Start();

            queuesCounts = CountQueuedPeople(players.Values);
        }


        public async Task AreYouReadyResponse(ConnectedUser user, AreYouReadyResponse response)
        {
            PlayerEntry entry;
            if (players.TryGetValue(user.Name, out entry))
                if (entry.InvitedToPlay)
                {
                    if (response.Ready) entry.LastReadyResponse = true;
                    else await RemoveUser(user.Name);

                    var invitedPeople = players.Values.Where(x => x?.InvitedToPlay == true).ToList();

                    if ((invitedPeople.Count == 0) || invitedPeople.All(x => x.LastReadyResponse)) OnTick();
                    else
                    {
                        var readyCounts = CountQueuedPeople(invitedPeople.Where(x => x.LastReadyResponse));

                        var proposedBattles = ProposeBattles(invitedPeople.Where(x => x.LastReadyResponse));

                        await Task.WhenAll(invitedPeople.Select(async (p) =>
                        {
                            var invitedBattle = invitationBattles?.FirstOrDefault(x => x.Players.Contains(p));
                            await
                                server.SendToUser(p.Name,
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
                }
        }


        public async Task OnLoginAccepted(ICommandSender client)
        {
            await client.SendCommand(new MatchMakerSetup() { PossibleQueues = possibleQueues });
        }

        public async Task QueueRequest(ConnectedUser user, MatchMakerQueueRequest cmd)
        {
            var banTime = BannedSeconds(user.Name);
            if (banTime != null)
            {
                await user.SendCommand(new MatchMakerStatus() { BannedSeconds = banTime });
                await user.Respond($"You are banned for {banTime}s from MatchMaker because you refused previous match");
                return;
            }

            var wantedQueueNames = cmd.Queues?.ToList() ?? new List<string>();
            var wantedQueues = possibleQueues.Where(x => wantedQueueNames.Contains(x.Name)).ToList();

            if (wantedQueues.Count == 0) // delete
            {
                await RemoveUser(user.Name);
                return;
            }

            var userEntry = players.AddOrUpdate(user.Name,
                (str) => new PlayerEntry(user.User, wantedQueues),
                (str, usr) =>
                {
                    usr.UpdateTypes(wantedQueues);
                    return usr;
                });

            queuesCounts = CountQueuedPeople(players.Values);
            await user.SendCommand(ToMatchMakerStatus(userEntry));

            if (!players.Values.Any(x => x?.InvitedToPlay == true)) OnTick(); // if nobody is invited, we can do tick now to speed up things
        }

        public async Task RemoveUser(string name)
        {
            PlayerEntry entry;
            if (players.TryRemove(name, out entry)) if (entry.InvitedToPlay) bannedPlayers[entry.Name] = DateTime.UtcNow; // was invited but he is gone now (whatever reason), ban!

            ConnectedUser conUser;
            if (server.ConnectedUsers.TryGetValue(name, out conUser) && (conUser != null))
            {
                if (entry?.InvitedToPlay == true) await conUser.SendCommand(new AreYouReadyResult() { AreYouBanned = true, IsBattleStarting = false, });

                await conUser.SendCommand(new MatchMakerStatus() { BannedSeconds = BannedSeconds(name) }); // left queue
            }
        }


        private int? BannedSeconds(string name)
        {
            DateTime banEntry;
            if (bannedPlayers.TryGetValue(name, out banEntry) && (DateTime.UtcNow.Subtract(banEntry).TotalMinutes < BanMinutes)) return (int)(BanMinutes * 60 - DateTime.UtcNow.Subtract(banEntry).TotalSeconds);
            else bannedPlayers.TryRemove(name, out banEntry);
            return null;
        }

        private Dictionary<string, int> CountQueuedPeople(IEnumerable<PlayerEntry> sumPlayers)
        {
            var ncounts = possibleQueues.ToDictionary(x => x.Name, x => 0);
            foreach (var plr in sumPlayers.Where(x => x != null)) foreach (var jq in plr.QueueTypes) ncounts[jq.Name]++;
            return ncounts;
        }

        private void OnTick()
        {
            lock (tickLock)
            {
                try
                {
                    timer.Stop();
                    var realBattles = ResolveToRealBattles();

                    queuesCounts = CountQueuedPeople(players.Values);

                    foreach (var bat in realBattles) StartBattle(bat);

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
        }

        private static List<ProposedBattle> ProposeBattles(IEnumerable<PlayerEntry> users)
        {
            var proposedBattles = new List<ProposedBattle>();

            var usersByWaitTime = users.OrderBy(x => x.JoinedTime).ToList();

            foreach (var user in usersByWaitTime)
            {
                if (proposedBattles.Any(y => y.Players.Contains(user))) continue; // skip already assigned in battles
                var battle = TryToMakeBattle(user, usersByWaitTime);
                if (battle != null) proposedBattles.Add(battle);
            }

            return proposedBattles;
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

            foreach (var plr in players.Values.Where(x => x != null))
            {
                ConnectedUser connectedUser;
                if (server.ConnectedUsers.TryGetValue(plr.Name, out connectedUser)) connectedUser?.SendCommand(ToMatchMakerStatus(plr));
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
            server.Broadcast(readyAndStarting.Select(x => x.Name), new MatchMakerStatus() { });

            foreach (var usr in readyAndStarting)
            {
                PlayerEntry entry;
                players.TryRemove(usr.Name, out entry);
            }

            return realBattles;
        }

        private async Task StartBattle(ProposedBattle bat)
        {
            var battleID = Interlocked.Increment(ref server.BattleCounter);

            var battle = new ServerBattle(server, true);
            battle.UpdateWith(new BattleHeader()
            {
                BattleID = battleID,
                Founder = "#MatchMaker_" + battleID,
                Engine = server.Engine,
                Game = server.Game,
                Title = "MatchMaker " + battleID,
                Mode = bat.Mode,
            });
            server.Battles[battleID] = battle;

            //foreach (var plr in bat.Players) battle.Users[plr.Name] = new UserBattleStatus(plr.Name, plr.LobbyUser) { IsSpectator = false, AllyNumber = 0, };

            // also join in lobby
            await server.Broadcast(server.ConnectedUsers.Keys, new BattleAdded() { Header = battle.GetHeader() });
            foreach (var usr in bat.Players) await server.ForceJoinBattle(usr.Name, battle);

            await battle.StartGame();
        }


        private void TimerTick(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            OnTick();
        }


        private MatchMakerStatus ToMatchMakerStatus(PlayerEntry entry)
        {
            return new MatchMakerStatus()
            {
                QueueCounts = queuesCounts,
                JoinedQueues = entry.QueueTypes.Select(x => x.Name).ToList(),
                CurrentEloWidth = entry.EloWidth,
                JoinedTime = entry.JoinedTime,
            };
        }

        private static ProposedBattle TryToMakeBattle(PlayerEntry player, IList<PlayerEntry> otherPlayers)
        {
            var playersByTeamElo =
                otherPlayers.Where(x => x != player).OrderBy(x => Math.Abs(x.LobbyUser.EffectiveElo - player.LobbyUser.EffectiveElo)).ToList();
            var playersBy1v1Elo =
                otherPlayers.Where(x => x != player).OrderBy(x => Math.Abs(x.LobbyUser.Effective1v1Elo - player.LobbyUser.Effective1v1Elo)).ToList();

            var testedBattles = player.GenerateWantedBattles();

            foreach (var other in playersByTeamElo)
                foreach (var bat in testedBattles.Where(x => x.Mode != AutohostMode.Game1v1))
                    if (bat.CanBeAdded(other))
                    {
                        bat.AddPlayer(other);
                        if (bat.Players.Count == bat.Size) return bat;
                    }

            foreach (var other in playersBy1v1Elo)
                foreach (var bat in testedBattles.Where(x => x.Mode == AutohostMode.Game1v1))
                    if (bat.CanBeAdded(other))
                    {
                        bat.AddPlayer(other);
                        if (bat.Players.Count == bat.Size) return bat;
                    }

            return null;
        }


        public class PlayerEntry
        {
            public bool InvitedToPlay;
            public bool LastReadyResponse;
            public int EloWidth => (int)Math.Min(400, 100 + DateTime.UtcNow.Subtract(JoinedTime).TotalSeconds / 30 * 50);
            public DateTime JoinedTime { get; private set; } = DateTime.UtcNow;
            public User LobbyUser { get; private set; }
            public string Name => LobbyUser.Name;
            public List<MatchMakerSetup.Queue> QueueTypes { get; private set; }


            public PlayerEntry(User user, List<MatchMakerSetup.Queue> queueTypes)
            {
                QueueTypes = queueTypes;
                LobbyUser = user;
            }

            public List<ProposedBattle> GenerateWantedBattles()
            {
                var ret = new List<ProposedBattle>();
                foreach (var qt in QueueTypes) for (var i = qt.MaxSize; i >= qt.MinSize; i--) ret.Add(new ProposedBattle(i, this, qt.Mode));
                return ret;
            }

            public void UpdateTypes(List<MatchMakerSetup.Queue> queueTypes)
            {
                QueueTypes = queueTypes;
            }
        }


        public class ProposedBattle
        {
            private PlayerEntry owner;
            public List<PlayerEntry> Players = new List<PlayerEntry>();
            public int Size;
            public int MaxElo { get; private set; } = int.MinValue;
            public int MinElo { get; private set; } = int.MaxValue;
            public AutohostMode Mode { get; private set; }

            public ProposedBattle(int size, PlayerEntry initialPlayer, AutohostMode mode)
            {
                Size = size;
                Mode = mode;
                owner = initialPlayer;
                AddPlayer(initialPlayer);
            }

            public void AddPlayer(PlayerEntry player)
            {
                Players.Add(player);
                var elo = GetElo(player);
                MinElo = Math.Min(MinElo, elo);
                MaxElo = Math.Max(MaxElo, elo);
            }

            public bool CanBeAdded(PlayerEntry other)
            {
                if (!other.GenerateWantedBattles().Any(y => (y.Size == Size) && (y.Mode == Mode))) return false;
                var widthMultiplier = Math.Max(1.0, 1.0 + (Size - 4) * 0.1);

                var elo = GetElo(other);
                if ((elo - MaxElo > owner.EloWidth * widthMultiplier) || (MinElo - elo > owner.EloWidth * widthMultiplier)) return false;

                return true;
            }

            private int GetElo(PlayerEntry entry)
            {
                if (Mode == AutohostMode.Game1v1) return entry.LobbyUser.Effective1v1Elo;
                return entry.LobbyUser.EffectiveElo;
            }
        }
    }
}