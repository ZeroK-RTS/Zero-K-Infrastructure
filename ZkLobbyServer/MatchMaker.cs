using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private ConcurrentDictionary<string, PlayerEntry> mmUsers = new ConcurrentDictionary<string, PlayerEntry>();

        private List<MatchMakerSetup.Queue> possibleQueues = new List<MatchMakerSetup.Queue>();
        private ZkLobbyServer server;

        private Timer timer;

        const int TimerSeconds = 30;

        public MatchMaker(ZkLobbyServer server)
        {
            this.server = server;
            using (var db = new ZkDataContext())
            {
                possibleQueues.Add(new MatchMakerSetup.Queue()
                {
                    Name = "1v1",
                    Description = "Duels with reasonable skill difference",
                    MaxPartySize = 1,
                    MinSize = 2,
                    MaxSize = 2,
                    Maps =
                        db.Resources.Where(
                                x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.MapIs1v1 == true) && (x.TypeID == ResourceType.Map))
                            .Select(x => x.InternalName)
                            .ToList()
                });

                possibleQueues.Add(new MatchMakerSetup.Queue()
                {
                    Name = "Teams",
                    Description = "Small teams 2v2 to 4v4 with reasonable skill difference",
                    MaxPartySize = 4,
                    MinSize = 4,
                    MaxSize = 8,
                    Maps =
                        db.Resources.Where(
                                x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.MapIsTeams == true) && (x.TypeID == ResourceType.Map))
                            .Select(x => x.InternalName)
                            .ToList()
                });
            }
            timer = new Timer(TimerSeconds * 1000);
            timer.AutoReset = true;
            timer.Elapsed += TimerOnElapsed;
            timer.Start();
        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var realBattles = ResolveToRealBattles();

            foreach (var bat in realBattles) StartBattle(bat);

            SendMmInvitations();
        }

        private List<ProposedBattle> ResolveToRealBattles()
        {
            var lastMatchedUsers = mmUsers.Values.Where(x => x?.InvitedToPlay == true).ToList();

            // force leave those not ready
            foreach (var pl in lastMatchedUsers.Where(x => !x.LastReadyResponse))
            {
                // todo also ban
                PlayerEntry entry;
                mmUsers.TryRemove(pl.Name, out entry);
                ConnectedUser conUser;
                if (server.ConnectedUsers.TryGetValue(pl.Name, out conUser)) conUser.SendCommand(new MatchMakerStatus()); // left queue
            }

            var readyUsers = lastMatchedUsers.Where(x => x.LastReadyResponse).ToList();
            var realBattles = ProposeBattles(readyUsers);
            var readyAndStarting = readyUsers.Where(x => realBattles.Any(y => y.Players.Contains(x))).ToList();
            server.Broadcast(readyUsers.Where(x => !realBattles.Any(y => y.Players.Contains(x))).Select(x => x.Name),
                new AreYouReady() { Text = "Match failed, someone else was not ready" });

            server.Broadcast(readyAndStarting.Select(x=>x.Name), new AreYouReady() { Text = "Match success, starting soon" });

            
            server.Broadcast(readyAndStarting.Select(x => x.Name), new MatchMakerStatus() { });
            foreach (var usr in readyAndStarting)
            {
                PlayerEntry entry;
                mmUsers.TryRemove(usr.Name, out entry);
            }
            return realBattles;
        }

        private void SendMmInvitations()
        {
            // generate next battles and send inviatation
            var proposedBattles = ProposeBattles(mmUsers.Values.Where(x => x != null));
            var toInvite = proposedBattles.SelectMany(x => x.Players).ToList();
            foreach (var usr in mmUsers.Values.Where(x => x != null))
            {
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
            }
            server.Broadcast(toInvite.Select(x => x.Name),
                new AreYouReady() { NeedReadyResponse = true, SecondsRemaining = TimerSeconds, Text = "Match found, are you ready?" });
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
                Mode = bat.Size == 2 ? AutohostMode.Game1v1 : AutohostMode.Teams,
            });
            server.Battles[battleID] = battle;

            foreach (var plr in bat.Players)
            {
                battle.Users[plr.Name] = new UserBattleStatus(plr.Name, plr.LobbyUser)
                {
                    IsSpectator = false,
                    AllyNumber = 0,
                };
            }

            await battle.StartGame();

            // also join in lobby
            await server.Broadcast(server.ConnectedUsers.Keys, new BattleAdded() { Header = battle.GetHeader() });
            foreach (var usr in bat.Players) await server.ForceJoinBattle(usr.Name, battle);
        }


        public async Task OnLoginAccepted(ICommandSender client)
        {
            await client.SendCommand(new MatchMakerSetup() { PossibleQueues = possibleQueues });
        }

        public async Task StartMatchMaker(ConnectedUser user, MatchMakerQueueRequest cmd)
        {
            var wantedQueueNames = cmd.Queues?.ToList() ?? new List<string>();
            var wantedQueues = possibleQueues.Where(x => wantedQueueNames.Contains(x.Name)).ToList();

            if (wantedQueues.Count == 0) // delete
            {
                PlayerEntry entry;
                mmUsers.TryRemove(user.Name, out entry);
                await user.SendCommand(new MatchMakerStatus()); // left queue
                return;
            }

            var userEntry = mmUsers.AddOrUpdate(user.Name,
                (str) => new PlayerEntry(user.User, wantedQueues),
                (str, usr) =>
                {
                    usr.UpdateTypes(wantedQueues);
                    return usr;
                });

            await user.SendCommand(ToMatchMakerStatus(userEntry));
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


        private MatchMakerStatus ToMatchMakerStatus(PlayerEntry entry)
        {
            return new MatchMakerStatus() { Text = "In queue", JoinedQueues = entry.QueueTypes.Select(x => x.Name).ToList() };
        }

        private static ProposedBattle TryToMakeBattle(PlayerEntry player, IList<PlayerEntry> otherPlayers)
        {
            var playersByTeamElo =
                otherPlayers.Where(x => x != player).OrderBy(x => Math.Abs(x.LobbyUser.EffectiveElo - player.LobbyUser.EffectiveElo)).ToList();
            var playersBy1v1Elo =
                otherPlayers.Where(x => x != player).OrderBy(x => Math.Abs(x.LobbyUser.Effective1v1Elo - player.LobbyUser.Effective1v1Elo)).ToList();

            var testedBattles = new List<ProposedBattle>();
            foreach (var size in player.WantedGameSizes.OrderByDescending(x => x)) testedBattles.Add(new ProposedBattle(size, player));

            foreach (var other in playersByTeamElo)
                foreach (var bat in testedBattles.Where(x => x.Size > 2))
                    if (bat.CanBeAdded(other))
                    {
                        bat.AddPlayer(other);
                        if (bat.Players.Count == bat.Size) return bat;
                    }

            foreach (var other in playersBy1v1Elo)
                foreach (var bat in testedBattles.Where(x => x.Size <= 2))
                    if (bat.CanBeAdded(other))
                    {
                        bat.AddPlayer(other);
                        if (bat.Players.Count == bat.Size) return bat;
                    }

            return null;
        }


        public class PlayerEntry
        {
            public int EloWidth => (int)Math.Min(400, 100 + DateTime.UtcNow.Subtract(JoinedTime).TotalSeconds / 30 * 50);
            public DateTime JoinedTime { get; private set; } = DateTime.UtcNow;
            public User LobbyUser { get; private set; }
            public string Name => LobbyUser.Name;
            public List<MatchMakerSetup.Queue> QueueTypes { get; private set; }
            public List<int> WantedGameSizes { get; private set; }
            public bool LastReadyResponse;
            public bool InvitedToPlay;


            public PlayerEntry(User user, List<MatchMakerSetup.Queue> queueTypes)
            {
                QueueTypes = queueTypes;
                LobbyUser = user;
                WantedGameSizes = GetWantedSizes();
            }

            public List<int> GetWantedSizes()
            {
                var ret = new List<int>();
                foreach (var qt in QueueTypes) for (var i = qt.MinSize; i <= qt.MaxSize; i++) ret.Add(i);
                return ret.Distinct().OrderByDescending(x => x).ToList();
            }

            public void UpdateTypes(List<MatchMakerSetup.Queue> queueTypes)
            {
                QueueTypes = queueTypes;
                WantedGameSizes = GetWantedSizes();
            }
        }


        public class ProposedBattle
        {
            private PlayerEntry owner;
            public List<PlayerEntry> Players = new List<PlayerEntry>();
            public int Size;
            public int MaxElo { get; private set; } = int.MinValue;
            public int MinElo { get; private set; } = int.MaxValue;

            public ProposedBattle(int size, PlayerEntry initialPlayer)
            {
                Size = size;
                owner = initialPlayer;
                AddPlayer(initialPlayer);
            }

            public void AddPlayer(PlayerEntry player)
            {
                Players.Add(player);
                var elo = Size > 2 ? player.LobbyUser.EffectiveElo : player.LobbyUser.Effective1v1Elo;
                MinElo = Math.Min(MinElo, elo);
                MaxElo = Math.Max(MaxElo, elo);
            }

            public bool CanBeAdded(PlayerEntry other)
            {
                if (!other.WantedGameSizes.Contains(Size)) return false;

                var elo = Size > 2 ? other.LobbyUser.EffectiveElo : other.LobbyUser.Effective1v1Elo;
                if ((elo - MaxElo > owner.EloWidth) || (MinElo - elo > owner.EloWidth)) return false;

                return true;
            }
        }

        public void RemoveUser(ConnectedUser connectedUser)
        {
            PlayerEntry entry;
            mmUsers.TryRemove(connectedUser.Name, out entry);
        }

        public async Task AreYouReadyResponse(ConnectedUser user, AreYouReadyResponse response)
        {
            PlayerEntry entry;
            if (mmUsers.TryGetValue(user.Name, out entry))
            {
                if (response.Ready) entry.LastReadyResponse = true;
                else
                {
                    PlayerEntry ent;
                    mmUsers.TryRemove(user.Name, out ent);
                    await user.SendCommand(new MatchMakerStatus());
                }
            }
        }
    }
}