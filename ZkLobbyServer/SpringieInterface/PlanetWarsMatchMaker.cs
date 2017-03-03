using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using Newtonsoft.Json;
using ZkData;
using ZkLobbyServer;

namespace ZeroKWeb
{
    /// <summary>
    ///     Handles arranging and starting of PW games
    /// </summary>
    public class PlanetWarsMatchMaker : PlanetWarsMatchMakerState
    {
        private readonly List<Faction> factions;


        private int missedDefenseCount = 0;
        private int missedDefenseFactionID = 0;
        private ZkLobbyServer.ZkLobbyServer server;


        private Timer timer;
        /// <summary>
        ///     Faction that should attack this turn
        /// </summary>
        [JsonIgnore]
        public Faction AttackingFaction { get { return factions[AttackerSideCounter % factions.Count]; } }

        public PlanetWarsMatchMaker(ZkLobbyServer.ZkLobbyServer server)
        {
            this.server = server;
            AttackOptions = new List<AttackOption>();
            RunningBattles = new Dictionary<int, AttackOption>();

            var db = new ZkDataContext();

            var gal = db.Galaxies.First(x => x.IsDefault);
            factions = db.Factions.Where(x => !x.IsDeleted).ToList();

            PlanetWarsMatchMakerState dbState = null;
            if (gal.MatchMakerState != null)
                try
                {
                    dbState = JsonConvert.DeserializeObject<PlanetWarsMatchMakerState>(gal.MatchMakerState);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            if (dbState != null)
            {
                AttackerSideCounter = dbState.AttackerSideCounter;
                AttackOptions = dbState.AttackOptions;
                Challenge = dbState.Challenge;
                ChallengeTime = dbState.ChallengeTime;
                AttackerSideChangeTime = dbState.AttackerSideChangeTime;
                RunningBattles = dbState.RunningBattles;
            }
            else
            {
                AttackerSideCounter = gal.AttackerSideCounter;
                AttackerSideChangeTime = gal.AttackerSideChangeTime ?? DateTime.UtcNow;
            }

            timer = new Timer(10000);
            timer.AutoReset = true;
            timer.Elapsed += TimerOnElapsed;
            timer.Start();
        }

        public async Task AcceptChallenge()
        {
            if (missedDefenseFactionID == Challenge.OwnerFactionID)
            {
                missedDefenseCount = 0;
                missedDefenseFactionID = 0;
            }

            var battle = new PlanetWarsServerBattle(server, Challenge);
            RunningBattles[battle.BattleID] = Challenge;
            server.Battles[battle.BattleID] = battle;

            // also join in lobby
            await server.Broadcast(server.ConnectedUsers.Keys, new BattleAdded() { Header = battle.GetHeader() });
            foreach (var usr in Challenge.Attackers.Union(Challenge.Defenders).Select(x => server.ConnectedUsers.Get(x)?.User)) await server.ForceJoinBattle(usr.Name, battle);

            await battle.StartGame();

            var text =
                $"Battle for planet {Challenge.Name} starts on zk://@join_player:{Challenge.Attackers.FirstOrDefault()}  Roster: {string.Join(",", Challenge.Attackers)} vs {string.Join(",", Challenge.Defenders)}";

            foreach (var fac in factions) await server.GhostChanSay(fac.Shortcut, text);

            AttackerSideCounter++;
            ResetAttackOptions();
        }

        /// <summary>
        ///     Invoked from web page
        /// </summary>
        /// <param name="planet"></param>
        public void AddAttackOption(Planet planet)
        {
            if (!AttackOptions.Any(x => x.PlanetID == planet.PlanetID) && (Challenge == null) && (planet.OwnerFactionID != AttackingFaction.FactionID))
            {
                InternalAddOption(planet);
                UpdateLobby();
            }
        }

        public PwMatchCommand GenerateLobbyCommand()
        {
            PwMatchCommand command;
            if (Challenge == null)
                command = new PwMatchCommand(PwMatchCommand.ModeType.Attack)
                {
                    Options = AttackOptions.Select(x => x.ToVoteOption(PwMatchCommand.ModeType.Attack)).ToList(),
                    DeadlineSeconds = (int)GetAttackDeadline().Subtract(DateTime.UtcNow).TotalSeconds,
                    AttackerFaction = AttackingFaction.Shortcut
                };
            else
                command = new PwMatchCommand(PwMatchCommand.ModeType.Defend)
                {
                    Options = new List<PwMatchCommand.VoteOption> { Challenge.ToVoteOption(PwMatchCommand.ModeType.Defend) },
                    DeadlineSeconds = (int)GetAcceptDeadline().Subtract(DateTime.UtcNow).TotalSeconds,
                    AttackerFaction = AttackingFaction.Shortcut,
                    DefenderFactions = GetDefendingFactions(Challenge).Select(x => x.Shortcut).ToList()
                };
            return command;
        }

        public async Task JoinPlanet(string name, int planetId)
        {
            var user = server.ConnectedUsers.Get(name)?.User;
            if (user != null)
            {
                var faction = factions.FirstOrDefault(x => x.Shortcut == user.Faction);
                if (faction == null)
                {
                    var db = new ZkDataContext(); // this is a fallback, should not be needed
                    var acc = Account.AccountByName(db, name);
                    faction = factions.FirstOrDefault(x => x.FactionID == acc.FactionID);
                }
                if (faction == AttackingFaction) await JoinPlanetAttack(planetId, name);
                else if ((Challenge != null) && GetDefendingFactions(Challenge).Contains(faction)) await JoinPlanetDefense(planetId, name);
            }
        }

        public async Task OnJoinPlanet(ConnectedUser conus, PwJoinPlanet args)
        {
            if (conus.User.CanUserPlanetWars()) await JoinPlanet(conus.Name, args.PlanetID);
        }

        public async Task OnLoginAccepted(ConnectedUser connectedUser)
        {
            var u = connectedUser.User;
            if (!string.IsNullOrEmpty(u.Faction) && (u.Level >= GlobalConst.MinPlanetWarsLevel) &&
                (Math.Max(u.EffectiveMmElo, u.EffectiveElo) > GlobalConst.MinPlanetWarsElo)) await UpdateLobby(u.Name);
        }

        public async Task OnUserDisconnected(string name)
        {
            if (Challenge == null)
            {
                if (AttackOptions.Count > 0)
                {
                    var sumRemoved = 0;
                    foreach (var aop in AttackOptions) sumRemoved += aop.Attackers.RemoveAll(x => x == name);
                    if (sumRemoved > 0) await UpdateLobby();
                }
            }
            else
            {
                var userName = name;
                if (Challenge.Defenders.RemoveAll(x => x == userName) > 0) await UpdateLobby();
            }
        }


        public void RemoveFromRunningBattles(int battleID)
        {
            RunningBattles.Remove(battleID);
        }

        public async Task UpdateLobby()
        {
            await
                server.Broadcast(
                    server.ConnectedUsers.Values.Where(
                        x =>
                            (x.User.Faction != null) && (x.User.Level >= GlobalConst.MinPlanetWarsLevel) &&
                            (Math.Max(x.User.EffectiveMmElo, x.User.EffectiveElo) >= GlobalConst.MinPlanetWarsLevel)),
                    GenerateLobbyCommand());
            SaveStateToDb();
        }

        public Task UpdateLobby(string player)
        {
            return server.ConnectedUsers.Get(player)?.SendCommand(GenerateLobbyCommand());
        }

        private DateTime GetAcceptDeadline()
        {
            return ChallengeTime.Value.AddMinutes(GlobalConst.PlanetWarsMinutesToAccept);
        }

        private DateTime GetAttackDeadline()
        {
            var extra = 0;
            if (missedDefenseFactionID == AttackingFaction.FactionID) extra = Math.Min(missedDefenseCount * GlobalConst.PlanetWarsMinutesToAttack, 60);
            return AttackerSideChangeTime.AddMinutes(GlobalConst.PlanetWarsMinutesToAttack + extra);
        }

        private List<Faction> GetDefendingFactions(AttackOption target)
        {
            if (target.OwnerFactionID != null) return new List<Faction> { factions.Find(x => x.FactionID == target.OwnerFactionID) };
            return factions.Where(x => x != AttackingFaction).ToList();
        }

        private void InternalAddOption(Planet planet)
        {
            AttackOptions.Add(new AttackOption
            {
                PlanetID = planet.PlanetID,
                Map = planet.Resource.InternalName,
                OwnerFactionID = planet.OwnerFactionID,
                Name = planet.Name,
                TeamSize = planet.TeamSize,
            });
        }

        private async Task JoinPlanetAttack(int targetPlanetId, string userName)
        {
            var attackOption = AttackOptions.Find(x => x.PlanetID == targetPlanetId);
            if (attackOption != null)
            {
                var user = server.ConnectedUsers.Get(userName)?.User;
                if (user != null)
                    using (var db = new ZkDataContext())
                    {
                        var account = db.Accounts.Find(user.AccountID);
                        if ((account != null) && (account.FactionID == AttackingFaction.FactionID) && account.CanPlayerPlanetWars())
                        {
                            // remove existing user from other options
                            foreach (var aop in AttackOptions) aop.Attackers.RemoveAll(x => x == userName);

                            // add user to this option
                            if (attackOption.Attackers.Count < attackOption.TeamSize)
                            {
                                attackOption.Attackers.Add(user.Name);
                                await server.GhostChanSay(user.Faction, $"{userName} joins attack on {attackOption.Name}");

                                if (attackOption.Attackers.Count == attackOption.TeamSize) StartChallenge(attackOption);
                                else await UpdateLobby();
                            }
                        }
                    }
            }
        }

        private async Task JoinPlanetDefense(int targetPlanetID, string userName)
        {
            if ((Challenge != null) && (Challenge.PlanetID == targetPlanetID) && (Challenge.Defenders.Count < Challenge.TeamSize))
            {
                var user = server.ConnectedUsers.Get(userName)?.User;
                if (user != null)
                {
                    var db = new ZkDataContext();
                    var account = db.Accounts.Find(user.AccountID);
                    if ((account != null) && GetDefendingFactions(Challenge).Any(y => y.FactionID == account.FactionID) &&
                        account.CanPlayerPlanetWars())
                        if (!Challenge.Defenders.Any(y => y == user.Name))
                        {
                            Challenge.Defenders.Add(user.Name);
                            await server.GhostChanSay(user.Faction, $"{userName} joins defense of {Challenge.Name}");

                            if (Challenge.Defenders.Count == Challenge.TeamSize) await AcceptChallenge();
                            else await UpdateLobby();
                        }
                }
            }
        }

        private void RecordPlanetwarsLoss(AttackOption option)
        {
            if (option.OwnerFactionID != null)
                if (option.OwnerFactionID == missedDefenseFactionID)
                {
                    missedDefenseCount++;
                }
                else
                {
                    missedDefenseCount = 0;
                    missedDefenseFactionID = option.OwnerFactionID.Value;
                }

            var message = string.Format("{0} won because nobody tried to defend", AttackingFaction.Name);
            foreach (var fac in factions) server.GhostChanSay(fac.Shortcut, message);

            var text = new StringBuilder();
            try
            {
                var db = new ZkDataContext();
                var playerIds = option.Attackers.Select(x => x).Union(option.Defenders.Select(x => x)).ToList();

                PlanetWarsTurnHandler.EndTurn(option.Map,
                    null,
                    db,
                    0,
                    db.Accounts.Where(x => playerIds.Contains(x.Name) && (x.Faction != null)).ToList(),
                    text,
                    null,
                    db.Accounts.Where(x => option.Attackers.Contains(x.Name) && (x.Faction != null)).ToList(),
                    server.PlanetWarsEventCreator);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                text.Append(ex);
            }
        }

        private void ResetAttackOptions()
        {
            AttackOptions.Clear();
            AttackerSideChangeTime = DateTime.UtcNow;
            Challenge = null;
            ChallengeTime = null;

            using (var db = new ZkDataContext())
            {
                var gal = db.Galaxies.First(x => x.IsDefault);
                var cnt = 2;
                var attacker = db.Factions.Single(x => x.FactionID == AttackingFaction.FactionID);
                var planets =
                    gal.Planets.Where(x => x.OwnerFactionID != AttackingFaction.FactionID)
                        .OrderByDescending(x => x.PlanetFactions.Where(y => y.FactionID == AttackingFaction.FactionID).Sum(y => y.Dropships))
                        .ThenByDescending(x => x.PlanetFactions.Where(y => y.FactionID == AttackingFaction.FactionID).Sum(y => y.Influence))
                        .ToList();
                // list of planets by attacker's influence

                foreach (var planet in planets)
                {
                    if (planet.CanMatchMakerPlay(attacker))
                    {
                        // pick only those where you can actually attack atm
                        InternalAddOption(planet);
                        cnt--;
                    }
                    if (cnt == 0) break;
                }

                if (!AttackOptions.Any(y => y.TeamSize == 2))
                {
                    var planet = planets.FirstOrDefault(x => (x.TeamSize == 2) && x.CanMatchMakerPlay(attacker));
                    if (planet != null) InternalAddOption(planet);
                }
            }

            UpdateLobby();

            server.GhostChanSay(AttackingFaction.Shortcut, "It's your turn! Select a planet to attack");
        }

        private void SaveStateToDb()
        {
            var db = new ZkDataContext();
            var gal = db.Galaxies.First(x => x.IsDefault);

            gal.MatchMakerState = JsonConvert.SerializeObject((PlanetWarsMatchMakerState)this);

            gal.AttackerSideCounter = AttackerSideCounter;
            gal.AttackerSideChangeTime = AttackerSideChangeTime;
            db.SaveChanges();
        }


        private void StartChallenge(AttackOption attackOption)
        {
            Challenge = attackOption;
            ChallengeTime = DateTime.UtcNow;
            AttackOptions.Clear();
            UpdateLobby();
        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                if (Challenge == null)
                {
                    // attack timer
                    if (DateTime.UtcNow > GetAttackDeadline())
                    {
                        AttackerSideCounter++;
                        ResetAttackOptions();
                    }
                }
                else
                {
                    // accept timer
                    if (DateTime.UtcNow > GetAcceptDeadline())
                        if ((Challenge.Defenders.Count >= Challenge.Attackers.Count - 1) && (Challenge.Defenders.Count > 0)) AcceptChallenge();
                        else
                        {
                            RecordPlanetwarsLoss(Challenge);
                            AttackerSideCounter++;
                            ResetAttackOptions();
                        }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public class AttackOption
        {
            public List<string> Attackers { get; set; }

            public List<string> Defenders { get; set; }
            public string Map { get; set; }
            public string Name { get; set; }
            public int? OwnerFactionID { get; set; }
            public int PlanetID { get; set; }
            public int TeamSize { get; set; }

            public AttackOption()
            {
                Attackers = new List<string>();
                Defenders = new List<string>();
            }

            public PwMatchCommand.VoteOption ToVoteOption(PwMatchCommand.ModeType mode)
            {
                var opt = new PwMatchCommand.VoteOption
                {
                    PlanetID = PlanetID,
                    PlanetName = Name,
                    Map = Map,
                    Count = mode == PwMatchCommand.ModeType.Attack ? Attackers.Count : Defenders.Count,
                    Needed = TeamSize
                };

                return opt;
            }
        }
    }
}