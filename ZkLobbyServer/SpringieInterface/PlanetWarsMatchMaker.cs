using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using Newtonsoft.Json;
using PlasmaShared;
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

            var gal = db.Galaxies.FirstOrDefault(x => x.IsDefault);
            if (gal == null) return;


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

            timer = new Timer(1045);
            timer.AutoReset = true;
            timer.Elapsed += TimerOnElapsed;
            timer.Start();
        }

        private async Task AcceptChallenge()
        {
            if (missedDefenseFactionID == Challenge.OwnerFactionID)
            {
                missedDefenseCount = 0;
                missedDefenseFactionID = 0;
            }

            // only really start if attackers are present, otherwise missed battle opportunity basically
            Challenge.Attackers = Challenge.Attackers.Where(x => server.ConnectedUsers.ContainsKey(x)).ToList();
            Challenge.Defenders = Challenge.Defenders.Where(x => server.ConnectedUsers.ContainsKey(x)).ToList();
            if (Challenge.Attackers.Any() || Challenge.Defenders.Any())
            {
                var battle = new PlanetWarsServerBattle(server, Challenge);
                await server.AddBattle(battle);
                RunningBattles[battle.BattleID] = Challenge;

                // also join in lobby
                foreach (var usr in Challenge.Attackers.Union(Challenge.Defenders)) await server.ForceJoinBattle(usr, battle);

                if (await battle.StartGame())
                {

                    var text =
                        $"Battle for planet {Challenge.Name} starts on zk://@join_player:{Challenge.Attackers.FirstOrDefault()}  Roster: {string.Join(",", Challenge.Attackers)} vs {string.Join(",", Challenge.Defenders)}";

                    foreach (var fac in factions) await server.GhostChanSay(fac.Shortcut, text);
                }
                else await server.RemoveBattle(battle);
            }

            AttackerSideCounter++;
            ResetAttackOptions();
        }

        /// <summary>
        ///     Invoked from web page
        /// </summary>
        /// <param name="planet"></param>
        public void AddAttackOption(Planet planet)
        {
            try
            {
                if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running) return;

                if (!AttackOptions.Any(x => x.PlanetID == planet.PlanetID) && (Challenge == null) &&
                    (planet.OwnerFactionID != AttackingFaction.FactionID))
                {
                    InternalAddOption(planet);
                    UpdateLobby();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars error adding option {0}: {1}", planet, ex);
            }
        }

        public PwMatchCommand GenerateLobbyCommand()
        {
            PwMatchCommand command = null;
            try
            {
                if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running) return new PwMatchCommand(PwMatchCommand.ModeType.Clear);

                if (Challenge == null)
                    command = new PwMatchCommand(PwMatchCommand.ModeType.Attack)
                    {
                        Options = AttackOptions.Select(x => x.ToVoteOption(PwMatchCommand.ModeType.Attack)).ToList(),
                        Deadline = GetAttackDeadline(),
                        DeadlineSeconds = (int)GetAttackDeadline().Subtract(DateTime.UtcNow).TotalSeconds,
                        AttackerFaction = AttackingFaction.Shortcut
                    };
                else
                    command = new PwMatchCommand(PwMatchCommand.ModeType.Defend)
                    {
                        Options = new List<PwMatchCommand.VoteOption> { Challenge.ToVoteOption(PwMatchCommand.ModeType.Defend) },
                        Deadline = GetAcceptDeadline(),
                        DeadlineSeconds = (int)GetAcceptDeadline().Subtract(DateTime.UtcNow).TotalSeconds,
                        AttackerFaction = AttackingFaction.Shortcut,
                        DefenderFactions = GetDefendingFactions(Challenge).Select(x => x.Shortcut).ToList()
                    };
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars {0}: {1}", nameof(GenerateLobbyCommand), ex);
            }
            return command;
        }

        private async Task JoinPlanet(string name, int planetId)
        {
            try
            {
                var user = server.ConnectedUsers.Get(name)?.User;
                if (user != null)
                {
                    var faction = factions.First(x => x.Shortcut == user.Faction);
                    if (faction == AttackingFaction) await JoinPlanetAttack(planetId, name);
                    else if ((Challenge != null) && GetDefendingFactions(Challenge).Any(y=>y.FactionID == faction.FactionID)) await JoinPlanetDefense(planetId, name);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars {0} {1} {2} : {3}", nameof(JoinPlanet), name, planetId, ex);
            }
        }

        public async Task OnJoinPlanet(ConnectedUser conus, PwJoinPlanet args)
        {
            if (MiscVar.PlanetWarsMode == PlanetWarsModes.Running)
            {
                if (conus.User.CanUserPlanetWars()) await JoinPlanet(conus.Name, args.PlanetID);
            }
        }

        public async Task OnLoginAccepted(ConnectedUser connectedUser)
        {
            await connectedUser.SendCommand(GeneratePwStatus());

            if (MiscVar.PlanetWarsMode == PlanetWarsModes.Running)
            {
                var u = connectedUser.User;
                if (u.CanUserPlanetWars()) await UpdateLobby(u.Name);
            }
        }

        public async Task OnUserDisconnected(string name)
        {
            try
            {
                if (MiscVar.PlanetWarsMode == PlanetWarsModes.Running)
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
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars OnUserDisconnected: {0}", ex);
            }
        }


        public void RemoveFromRunningBattles(int battleID)
        {
            RunningBattles.Remove(battleID);
        }

        private async Task UpdateLobby()
        {
            await
                server.Broadcast(server.ConnectedUsers.Values.Where(x => x.User.CanUserPlanetWars()), GenerateLobbyCommand());
            SaveStateToDb();
        }

        private Task UpdateLobby(string player)
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
            if (AttackOptions.Count == 0)
            {
                return AttackerSideChangeTime.AddMinutes(GlobalConst.PlanetWarsMinutesToAttackIfNoOption);
            }

            if (missedDefenseFactionID == AttackingFaction.FactionID) extra = Math.Min(missedDefenseCount * GlobalConst.PlanetWarsMinutesToAttack, 60);

            return AttackerSideChangeTime.AddMinutes(GlobalConst.PlanetWarsMinutesToAttack + extra);
        }

        public List<Faction> GetDefendingFactions(AttackOption target)
        {
            if (target.OwnerFactionID != null)
            {
                var ret = new List<Faction>();
                ret.Add(factions.Find(x => x.FactionID == target.OwnerFactionID));

                // add allies as defenders
                using (var db = new ZkDataContext())
                {
                    var planet = db.Planets.Find(target.PlanetID);
                    foreach (var of in db.Factions.Where(x=>!x.IsDeleted && x.FactionID != target.OwnerFactionID && x.FactionID != AttackingFaction.FactionID))
                    {
                        if (of.GaveTreatyRight(planet, x=>x.EffectBalanceSameSide == true)) ret.Add(factions.First(x=>x.FactionID == of.FactionID));
                    }
                }
                return ret;
            }

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
                PlanetImage = planet.Resource?.MapPlanetWarsIcon,
                IconSize = planet.Resource?.PlanetWarsIconSize ?? 0,
                StructureImages = planet.PlanetStructures.Select(x => x.IsActive ? x.StructureType.MapIcon : x.StructureType.DisabledMapIcon).ToList()
            });
        }

        private async Task JoinPlanetAttack(int targetPlanetId, string userName)
        {
            var attackOption = AttackOptions.Find(x => x.PlanetID == targetPlanetId);
            if (attackOption != null)
            {
                var conus = server.ConnectedUsers.Get(userName);
                var user = conus?.User;
                if (user != null)
                    using (var db = new ZkDataContext())
                    {
                        var account = db.Accounts.Find(user.AccountID);
                        if ((account != null) && (account.FactionID == AttackingFaction.FactionID) && account.CanPlayerPlanetWars())
                        {
                            // remove existing user from other options
                            foreach (var aop in AttackOptions.Where(x => x.PlanetID != targetPlanetId)) aop.Attackers.RemoveAll(x => x == userName);

                            // add user to this option
                            if (attackOption.Attackers.Count < attackOption.TeamSize && !attackOption.Attackers.Contains(userName))
                            {
                                attackOption.Attackers.Add(user.Name);
                                await server.GhostChanSay(user.Faction, $"{userName} joins attack on {attackOption.Name}");

                                await conus.SendCommand(new PwJoinPlanetSuccess() { PlanetID = targetPlanetId });

                                if (attackOption.Attackers.Count == attackOption.TeamSize) await StartChallenge(attackOption);
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
                var conus = server.ConnectedUsers.Get(userName);
                var user = conus?.User;
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

                            await conus.SendCommand(new PwJoinPlanetSuccess() { PlanetID = targetPlanetID });

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
                    server.PlanetWarsEventCreator, server);
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
                var cnt = 6;
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


        private async Task StartChallenge(AttackOption attackOption)
        {
            Challenge = attackOption;
            ChallengeTime = DateTime.UtcNow;
            AttackOptions.Clear();
            await UpdateLobby();
            await server.Broadcast(attackOption.Attackers, new PwAttackingPlanet() { PlanetID = attackOption.PlanetID });
        }

        private PlanetWarsModes? lastPlanetWarsMode;

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                timer.Stop();

                // auto change PW mode based on time
                if (MiscVar.PlanetWarsNextModeTime != null && MiscVar.PlanetWarsNextModeTime < DateTime.UtcNow && MiscVar.PlanetWarsNextMode != null)
                {
                    MiscVar.PlanetWarsMode = MiscVar.PlanetWarsNextMode ?? PlanetWarsModes.AllOffline;

                    MiscVar.PlanetWarsNextMode = null;
                    MiscVar.PlanetWarsNextModeTime = null;

                    using (var db = new ZkDataContext())
                    {
                        db.Events.Add(server.PlanetWarsEventCreator.CreateEvent("PlanetWars changed status to {0}", MiscVar.PlanetWarsMode.Description()));
                        db.SaveChanges();
                    }
                }


                if (MiscVar.PlanetWarsMode != lastPlanetWarsMode)
                {
                    server.Broadcast(GeneratePwStatus());
                    UpdateLobby();
                    lastPlanetWarsMode = MiscVar.PlanetWarsMode;
                }

                if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running) return;

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
                Trace.TraceError("PlanetWars timer error: {0}", ex);
            }
            finally
            {
                timer.Start();
            }
        }

        private static PwStatus GeneratePwStatus()
        {
            return new PwStatus()
            {
                PlanetWarsMode = MiscVar.PlanetWarsMode,
                MinLevel = GlobalConst.MinPlanetWarsLevel,
                PlanetWarsNextMode = MiscVar.PlanetWarsNextMode,
                PlanetWarsNextModeTime = MiscVar.PlanetWarsNextModeTime
            };
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
            public List<string> StructureImages { get; set; } = new List<string>();
            public int IconSize { get; set; }


            public string PlanetImage { get; set; }

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
                    IconSize = IconSize,
                    StructureImages = StructureImages,
                    PlanetImage = PlanetImage,
                    Count = mode == PwMatchCommand.ModeType.Attack ? Attackers.Count : Defenders.Count,
                    Needed = TeamSize
                };

                return opt;
            }

        }
    }
}