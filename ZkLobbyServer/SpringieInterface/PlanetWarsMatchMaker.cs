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
using Ratings;
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

        private ZkLobbyServer.ZkLobbyServer server;
        private DateTime? defendersFullTime; // set when total defenders >= total attacker slots

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
            FormedSquads = new List<AttackOption>();
            DefenderVotes = new Dictionary<int, List<string>>();
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
                AttackOptions = dbState.AttackOptions ?? new List<AttackOption>();
                Phase = dbState.Phase;
                PhaseStartTime = dbState.PhaseStartTime;
                FormedSquads = dbState.FormedSquads ?? new List<AttackOption>();
                DefenderVotes = dbState.DefenderVotes ?? new Dictionary<int, List<string>>();
                AttackerSideChangeTime = dbState.AttackerSideChangeTime;
                RunningBattles = dbState.RunningBattles ?? new Dictionary<int, AttackOption>();
            }
            else
            {
                AttackerSideCounter = gal.AttackerSideCounter;
                AttackerSideChangeTime = gal.AttackerSideChangeTime ?? DateTime.UtcNow;
                Phase = PwPhase.AttackCollect;
                PhaseStartTime = DateTime.UtcNow;
            }

            timer = new Timer(1045);
            timer.AutoReset = true;
            timer.Elapsed += TimerOnElapsed;
            timer.Start();
        }


        // ===================== TIMER / STATE MACHINE =====================

        private PlanetWarsModes? lastPlanetWarsMode;

        private async void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
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

                // clean up stale running battles (e.g. if Spring process crashed)
                var staleBattleIds = RunningBattles.Keys.Where(id => !server.Battles.ContainsKey(id)).ToList();
                foreach (var id in staleBattleIds) RunningBattles.Remove(id);

                switch (Phase)
                {
                    case PwPhase.AttackCollect:
                        if (DateTime.UtcNow > GetAttackDeadline())
                        {
                            RunSquadFormation();
                            if (FormedSquads.Any())
                            {
                                // transition to defend
                                Phase = PwPhase.DefendCollect;
                                PhaseStartTime = DateTime.UtcNow;
                                UpdateLobby();
                            }
                            else
                            {
                                // nobody attacked, skip to next faction
                                AttackerSideCounter++;
                                ResetAttackOptions();
                            }
                        }
                        break;

                    case PwPhase.DefendCollect:
                        // check if enough defenders volunteered — start 30s countdown
                        var totalSlots = FormedSquads.Sum(s => s.TeamSize);
                        var totalDefenders = DefenderVotes.Values.Sum(v => v.Count);
                        if (totalDefenders >= totalSlots)
                        {
                            if (defendersFullTime == null) defendersFullTime = DateTime.UtcNow;
                        }
                        else
                        {
                            defendersFullTime = null;
                        }

                        var deadline = GetDefendDeadline();
                        if (defendersFullTime != null)
                        {
                            var earlyDeadline = defendersFullTime.Value.AddSeconds(30);
                            if (earlyDeadline < deadline) deadline = earlyDeadline;
                        }

                        if (DateTime.UtcNow > deadline)
                        {
                            defendersFullTime = null;
                            RunDefenderAssignment();
                            await LaunchAllBattles();
                            RunGalaxyTick();
                            AttackerSideCounter++;
                            ResetAttackOptions();
                        }
                        break;
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


        // ===================== SQUAD FORMATION (PIERCING) =====================

        private void RunSquadFormation()
        {
            FormedSquads.Clear();

            // collect all attackers still connected, grouped by planet
            var playerPlanet = new Dictionary<string, AttackOption>(); // player -> their chosen option
            foreach (var opt in AttackOptions)
            {
                opt.Attackers = opt.Attackers.Where(x => server.ConnectedUsers.ContainsKey(x)).ToList();
                foreach (var name in opt.Attackers)
                    playerPlanet[name] = opt;
            }

            if (!playerPlanet.Any()) return;

            // look up PW-WHR and PW-Rank for each player
            var playerWhr = new Dictionary<string, double>();
            var playerRoleOrder = new Dictionary<string, int>(); // lower = higher faction rank
            using (var db = new ZkDataContext())
            {
                foreach (var name in playerPlanet.Keys.ToList())
                {
                    var user = server.ConnectedUsers.Get(name)?.User;
                    if (user == null) { playerPlanet.Remove(name); continue; }

                    playerWhr[name] = GetPlayerWhr(name);

                    // PW-Rank: faction role DisplayOrder, lower = higher rank. No role = int.MaxValue
                    var account = db.Accounts.Find(user.AccountID);
                    var factionRole = account?.AccountRolesByAccountID
                        .Where(r => r.RoleType != null && !r.RoleType.IsClanOnly && r.RoleType.RestrictFactionID == AttackingFaction.FactionID)
                        .Select(r => r.RoleType.DisplayOrder)
                        .OrderBy(x => x)
                        .Cast<int?>()
                        .FirstOrDefault();
                    playerRoleOrder[name] = factionRole ?? int.MaxValue;
                }
            }

            var pool = new HashSet<string>(playerPlanet.Keys);

            // Pass 1: while any planet has >= TeamSize players, form squads from top WHR
            bool formed;
            do
            {
                formed = false;
                foreach (var opt in AttackOptions)
                {
                    var available = opt.Attackers.Where(pool.Contains).OrderByDescending(x => playerWhr.Get(x)).ToList();
                    while (available.Count >= opt.TeamSize)
                    {
                        var squad = CreateSquadFromOption(opt);
                        squad.Attackers = available.Take(opt.TeamSize).ToList();
                        FormedSquads.Add(squad);
                        foreach (var p in squad.Attackers) pool.Remove(p);
                        available = available.Skip(opt.TeamSize).ToList();
                        formed = true;
                    }
                }
            } while (formed); // repeat in case removing players from one planet frees up nothing, but be safe

            // Pass 2: piercing — top PW-Rank player pulls others to their planet
            while (pool.Count > 0)
            {
                // find top PW-Rank player (lowest DisplayOrder, tiebreak by WHR desc)
                var leader = pool
                    .OrderBy(x => playerRoleOrder.GetOrDefault(x, int.MaxValue))
                    .ThenByDescending(x => playerWhr.Get(x))
                    .First();

                var leaderOption = playerPlanet[leader];
                if (pool.Count < leaderOption.TeamSize)
                    break; // not enough players for any squad

                var fillers = pool
                    .Where(x => x != leader)
                    .OrderByDescending(x => playerWhr.Get(x))
                    .Take(leaderOption.TeamSize - 1)
                    .ToList();

                if (fillers.Count < leaderOption.TeamSize - 1)
                    break; // not enough

                var squad = CreateSquadFromOption(leaderOption);
                squad.Attackers = new List<string> { leader };
                squad.Attackers.AddRange(fillers);
                FormedSquads.Add(squad);

                pool.Remove(leader);
                foreach (var p in fillers) pool.Remove(p);
            }

            AttackOptions.Clear();

            // initialize defender votes for attacked planets
            DefenderVotes.Clear();
            foreach (var planetId in FormedSquads.Select(s => s.PlanetID).Distinct())
                DefenderVotes[planetId] = new List<string>();

            // notify attackers
            foreach (var squad in FormedSquads)
                server.Broadcast(squad.Attackers, new PwAttackingPlanet() { PlanetID = squad.PlanetID });
        }

        private AttackOption CreateSquadFromOption(AttackOption source)
        {
            return new AttackOption
            {
                PlanetID = source.PlanetID,
                Map = source.Map,
                Name = source.Name,
                OwnerFactionID = source.OwnerFactionID,
                TeamSize = source.TeamSize,
                PlanetImage = source.PlanetImage,
                IconSize = source.IconSize,
                StructureImages = source.StructureImages,
                Attackers = new List<string>(),
                Defenders = new List<string>()
            };
        }


        // ===================== DEFENDER ASSIGNMENT =====================

        private void RunDefenderAssignment()
        {
            // look up defender WHR
            var defenderWhr = new Dictionary<string, double>();
            foreach (var kv in DefenderVotes)
            {
                foreach (var name in kv.Value)
                {
                    if (defenderWhr.ContainsKey(name)) continue;
                    if (!server.ConnectedUsers.ContainsKey(name)) continue;
                    defenderWhr[name] = GetPlayerWhr(name);
                }
            }

            // per-planet: assign defenders, overflow to pool
            var floatingPool = new List<string>();
            var assignedDefenders = new Dictionary<int, List<string>>(); // planetID -> assigned defender names
            var attackedPlanetIds = FormedSquads.Select(s => s.PlanetID).Distinct().ToList();

            foreach (var planetId in attackedPlanetIds)
            {
                var totalSlotsNeeded = FormedSquads.Where(s => s.PlanetID == planetId).Sum(s => s.TeamSize);
                var volunteers = (DefenderVotes.ContainsKey(planetId) ? DefenderVotes[planetId] : new List<string>())
                    .Where(x => server.ConnectedUsers.ContainsKey(x) && defenderWhr.ContainsKey(x))
                    .OrderByDescending(x => defenderWhr[x])
                    .ToList();

                if (volunteers.Count > totalSlotsNeeded)
                {
                    assignedDefenders[planetId] = volunteers.Take(totalSlotsNeeded).ToList();
                    floatingPool.AddRange(volunteers.Skip(totalSlotsNeeded));
                }
                else
                {
                    assignedDefenders[planetId] = volunteers;
                }
            }

            // floating pool fills unfilled slots on other planets (WHR order)
            floatingPool = floatingPool.OrderByDescending(x => defenderWhr.Get(x)).ToList();
            foreach (var planetId in attackedPlanetIds)
            {
                var totalSlotsNeeded = FormedSquads.Where(s => s.PlanetID == planetId).Sum(s => s.TeamSize);
                var assigned = assignedDefenders[planetId];
                var deficit = totalSlotsNeeded - assigned.Count;
                if (deficit > 0 && floatingPool.Count > 0)
                {
                    var toAdd = floatingPool.Take(deficit).ToList();
                    assigned.AddRange(toAdd);
                    foreach (var p in toAdd) floatingPool.Remove(p);
                }
            }

            // slice defenders into squads: sort squads by avg attacker WHR desc, assign best defenders to best attackers
            foreach (var planetId in attackedPlanetIds)
            {
                var squadsForPlanet = FormedSquads
                    .Where(s => s.PlanetID == planetId)
                    .OrderByDescending(s => s.Attackers.Average(a => GetPlayerWhr(a))) // sort by attacker strength
                    .ToList();

                var defenders = assignedDefenders.ContainsKey(planetId)
                    ? assignedDefenders[planetId].OrderByDescending(x => defenderWhr.Get(x)).ToList()
                    : new List<string>();

                int idx = 0;
                foreach (var squad in squadsForPlanet)
                {
                    var count = Math.Min(squad.TeamSize, defenders.Count - idx);
                    if (count > 0)
                    {
                        squad.Defenders = defenders.Skip(idx).Take(count).ToList();
                        idx += count;
                    }
                    // else: no defenders at all for this squad (concede)
                }
            }
        }

        private double GetPlayerWhr(string name)
        {
            var user = server.ConnectedUsers.Get(name)?.User;
            if (user == null) return 0;
            return RatingSystems.GetRatingSystem(RatingCategory.Planetwars).GetPlayerRating(user.AccountID).LadderElo;
        }


        // ===================== LAUNCH BATTLES =====================

        private async Task LaunchAllBattles()
        {
            foreach (var squad in FormedSquads)
            {
                // filter to still-connected
                squad.Attackers = squad.Attackers.Where(x => server.ConnectedUsers.ContainsKey(x)).ToList();
                squad.Defenders = squad.Defenders.Where(x => server.ConnectedUsers.ContainsKey(x)).ToList();

                if (squad.Defenders.Count > 0 && squad.Attackers.Count > 0)
                {
                    // battle (may be uneven)
                    try
                    {
                        var battle = new PlanetWarsServerBattle(server, squad);
                        await server.AddBattle(battle);
                        RunningBattles[battle.BattleID] = squad;

                        foreach (var usr in squad.Attackers.Union(squad.Defenders))
                            await server.ForceJoinBattle(usr, battle);

                        if (await battle.StartGame())
                        {
                            var text = $"Battle for planet {squad.Name} starts on zk://@join_player:{squad.Attackers.FirstOrDefault()}  Roster: {string.Join(",", squad.Attackers)} vs {string.Join(",", squad.Defenders)}";
                            foreach (var fac in factions) await server.GhostChanSay(fac.Shortcut, text);
                        }
                        else
                        {
                            await server.RemoveBattle(battle);
                            RunningBattles.Remove(battle.BattleID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("PlanetWars LaunchBattle error: {0}", ex);
                    }
                }
                else if (squad.Attackers.Count > 0)
                {
                    // concede - zero defenders
                    RecordPlanetwarsLoss(squad);
                }
                // else: no attackers left, skip entirely
            }

            FormedSquads.Clear();
            DefenderVotes.Clear();
        }


        // ===================== GALAXY TICK =====================

        private void RunGalaxyTick()
        {
            try
            {
                var text = new StringBuilder();
                using (var db = new ZkDataContext())
                {
                    PlanetWarsTurnHandler.ProcessGalaxyTick(db, text, server.PlanetWarsEventCreator, server);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars galaxy tick error: {0}", ex);
            }
        }


        // ===================== PLAYER ACTIONS =====================

        public async Task OnJoinPlanet(ConnectedUser conus, PwJoinPlanet args)
        {
            if (MiscVar.PlanetWarsMode == PlanetWarsModes.Running)
            {
                if (conus.User.CanUserPlanetWars()) await JoinPlanet(conus.Name, args.PlanetID);
            }
        }

        private async Task JoinPlanet(string name, int planetId)
        {
            try
            {
                var user = server.ConnectedUsers.Get(name)?.User;
                if (user == null) return;

                var faction = factions.FirstOrDefault(x => x.Shortcut == user.Faction);
                if (faction == null) return;

                if (Phase == PwPhase.AttackCollect && faction == AttackingFaction)
                    await JoinPlanetAttack(planetId, name);
                else if (Phase == PwPhase.DefendCollect && faction != AttackingFaction)
                    await JoinPlanetDefense(planetId, name);
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars {0} {1} {2} : {3}", nameof(JoinPlanet), name, planetId, ex);
            }
        }

        private async Task JoinPlanetAttack(int targetPlanetId, string userName)
        {
            var attackOption = AttackOptions.Find(x => x.PlanetID == targetPlanetId);
            if (attackOption == null) return;

            var conus = server.ConnectedUsers.Get(userName);
            var user = conus?.User;
            if (user == null) return;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.Find(user.AccountID);
                if (account == null || account.FactionID != AttackingFaction.FactionID || !account.CanPlayerPlanetWars()) return;

                // remove from other options
                foreach (var aop in AttackOptions.Where(x => x.PlanetID != targetPlanetId))
                    aop.Attackers.RemoveAll(x => x == userName);

                // add to this option (no cap — it's a vote, squad formation handles sizing)
                if (!attackOption.Attackers.Contains(userName))
                {
                    attackOption.Attackers.Add(user.Name);
                    await server.GhostChanSay(user.Faction, $"{userName} joins attack on {attackOption.Name}");
                    await conus.SendCommand(new PwJoinPlanetSuccess() { PlanetID = targetPlanetId });
                    await UpdateLobby();
                }
            }
        }

        private async Task JoinPlanetDefense(int targetPlanetId, string userName)
        {
            if (!DefenderVotes.ContainsKey(targetPlanetId)) return;

            var conus = server.ConnectedUsers.Get(userName);
            var user = conus?.User;
            if (user == null) return;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.Find(user.AccountID);
                if (account == null || !account.CanPlayerPlanetWars()) return;

                // check this user's faction can defend at least one squad on this planet
                var squadsOnPlanet = FormedSquads.Where(s => s.PlanetID == targetPlanetId).ToList();
                if (!squadsOnPlanet.Any()) return;
                var defendingFactions = GetDefendingFactions(squadsOnPlanet.First());
                if (!defendingFactions.Any(f => f.FactionID == account.FactionID)) return;

                // remove from other planets
                foreach (var kv in DefenderVotes)
                    kv.Value.RemoveAll(x => x == userName);

                // add to this planet
                if (!DefenderVotes[targetPlanetId].Contains(userName))
                {
                    DefenderVotes[targetPlanetId].Add(userName);
                    await server.GhostChanSay(user.Faction, $"{userName} joins defense of {squadsOnPlanet.First().Name}");
                    await conus.SendCommand(new PwJoinPlanetSuccess() { PlanetID = targetPlanetId });
                    await UpdateLobby();
                }
            }
        }


        // ===================== CONNECTION EVENTS =====================

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
                if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running) return;

                bool changed = false;
                if (Phase == PwPhase.AttackCollect)
                {
                    foreach (var aop in AttackOptions)
                        changed |= aop.Attackers.RemoveAll(x => x == name) > 0;
                }
                else if (Phase == PwPhase.DefendCollect)
                {
                    // remove from defender votes
                    foreach (var kv in DefenderVotes)
                        changed |= kv.Value.RemoveAll(x => x == name) > 0;

                    // also remove from formed squads (attacker who disconnected after squad formation)
                    foreach (var squad in FormedSquads)
                        changed |= squad.Attackers.RemoveAll(x => x == name) > 0;
                }

                if (changed) await UpdateLobby();
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars OnUserDisconnected: {0}", ex);
            }
        }


        // ===================== LOBBY COMMANDS =====================

        public PwMatchCommand GenerateLobbyCommand()
        {
            PwMatchCommand command = null;
            try
            {
                if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running)
                    return new PwMatchCommand(PwMatchCommand.ModeType.Clear);

                if (Phase == PwPhase.AttackCollect)
                {
                    command = new PwMatchCommand(PwMatchCommand.ModeType.Attack)
                    {
                        Options = AttackOptions.Select(x => x.ToVoteOption(PwMatchCommand.ModeType.Attack)).ToList(),
                        Deadline = GetAttackDeadline(),
                        DeadlineSeconds = (int)GetAttackDeadline().Subtract(DateTime.UtcNow).TotalSeconds,
                        AttackerFaction = AttackingFaction.Shortcut
                    };
                }
                else if (Phase == PwPhase.DefendCollect)
                {
                    // aggregate per planet: one VoteOption per planet showing total slots needed
                    var options = new List<PwMatchCommand.VoteOption>();
                    foreach (var planetId in FormedSquads.Select(s => s.PlanetID).Distinct())
                    {
                        var squads = FormedSquads.Where(s => s.PlanetID == planetId).ToList();
                        var first = squads.First();
                        var totalNeeded = squads.Sum(s => s.TeamSize);
                        var volunteered = DefenderVotes.ContainsKey(planetId) ? DefenderVotes[planetId].Count : 0;

                        options.Add(new PwMatchCommand.VoteOption
                        {
                            PlanetID = first.PlanetID,
                            PlanetName = first.Name,
                            Map = first.Map,
                            IconSize = first.IconSize,
                            StructureImages = first.StructureImages,
                            PlanetImage = first.PlanetImage,
                            Count = volunteered,
                            Needed = totalNeeded
                        });
                    }

                    // collect all defending factions across attacked planets (one DB call per distinct planet, not per squad)
                    var defFactionCache = new Dictionary<int, List<Faction>>();
                    foreach (var pid in options.Select(o => o.PlanetID))
                    {
                        if (!defFactionCache.ContainsKey(pid))
                            defFactionCache[pid] = GetDefendingFactions(FormedSquads.First(s => s.PlanetID == pid));
                    }
                    var allDefFactions = defFactionCache.Values
                        .SelectMany(f => f.Select(x => x.Shortcut))
                        .Distinct()
                        .ToList();

                    command = new PwMatchCommand(PwMatchCommand.ModeType.Defend)
                    {
                        Options = options,
                        Deadline = GetDefendDeadline(),
                        DeadlineSeconds = (int)GetDefendDeadline().Subtract(DateTime.UtcNow).TotalSeconds,
                        AttackerFaction = AttackingFaction.Shortcut,
                        DefenderFactions = allDefFactions
                    };
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars {0}: {1}", nameof(GenerateLobbyCommand), ex);
            }
            return command;
        }


        // ===================== ATTACK OPTIONS =====================

        /// <summary>
        ///     Invoked from web page
        /// </summary>
        public void AddAttackOption(Planet planet)
        {
            try
            {
                if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running) return;
                if (Phase != PwPhase.AttackCollect) return;

                if (!AttackOptions.Any(x => x.PlanetID == planet.PlanetID) &&
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

        private void ResetAttackOptions()
        {
            AttackOptions.Clear();
            FormedSquads.Clear();
            DefenderVotes.Clear();
            Phase = PwPhase.AttackCollect;
            PhaseStartTime = DateTime.UtcNow;
            AttackerSideChangeTime = DateTime.UtcNow;

            var contestedPlanetIds = RunningBattles.Values.Select(x => x.PlanetID).ToHashSet();

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

                foreach (var planet in planets)
                {
                    if (planet.CanMatchMakerPlay(attacker) && !contestedPlanetIds.Contains(planet.PlanetID))
                    {
                        InternalAddOption(planet);
                        cnt--;
                    }
                    if (cnt == 0) break;
                }

                if (!AttackOptions.Any(y => y.TeamSize == 2))
                {
                    var planet = planets.FirstOrDefault(x => (x.TeamSize == 2) && x.CanMatchMakerPlay(attacker) && !contestedPlanetIds.Contains(x.PlanetID));
                    if (planet != null) InternalAddOption(planet);
                }
            }

            UpdateLobby();
            server.GhostChanSay(AttackingFaction.Shortcut, "It's your turn! Select a planet to attack");
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


        // ===================== HELPERS =====================

        public List<Faction> GetDefendingFactions(AttackOption target)
        {
            if (target.OwnerFactionID != null)
            {
                var ret = new List<Faction>();
                ret.Add(factions.Find(x => x.FactionID == target.OwnerFactionID));

                using (var db = new ZkDataContext())
                {
                    var planet = db.Planets.Find(target.PlanetID);
                    foreach (var of in db.Factions.Where(x => !x.IsDeleted && x.FactionID != target.OwnerFactionID && x.FactionID != AttackingFaction.FactionID))
                    {
                        if (of.GaveTreatyRight(planet, x => x.EffectBalanceSameSide == true))
                            ret.Add(factions.First(x => x.FactionID == of.FactionID));
                    }
                }
                return ret;
            }

            return factions.Where(x => x != AttackingFaction).ToList();
        }

        private void RecordPlanetwarsLoss(AttackOption option)
        {
            var message = $"{AttackingFaction.Name} won {option.Name} because nobody tried to defend";
            foreach (var fac in factions) server.GhostChanSay(fac.Shortcut, message);

            try
            {
                using (var db = new ZkDataContext())
                {
                    var playerIds = option.Attackers.Union(option.Defenders).ToList();

                    PlanetWarsTurnHandler.ProcessBattleResult(option.Map,
                        null,
                        db,
                        0,
                        db.Accounts.Where(x => playerIds.Contains(x.Name) && (x.Faction != null)).ToList(),
                        new StringBuilder(),
                        null,
                        db.Accounts.Where(x => option.Attackers.Contains(x.Name) && (x.Faction != null)).ToList(),
                        server.PlanetWarsEventCreator, server);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars RecordLoss error: {0}", ex);
            }
        }

        private DateTime GetAttackDeadline()
        {
            if (AttackOptions.Count == 0)
                return PhaseStartTime.AddMinutes(GlobalConst.PlanetWarsMinutesToAttackIfNoOption);

            return PhaseStartTime.AddMinutes(GlobalConst.PlanetWarsMinutesToAttack);
        }

        private DateTime GetDefendDeadline()
        {
            return PhaseStartTime.AddMinutes(GlobalConst.PlanetWarsMinutesToAccept);
        }

        public void RemoveFromRunningBattles(int battleID)
        {
            RunningBattles.Remove(battleID);
        }

        private async Task UpdateLobby()
        {
            await server.Broadcast(server.ConnectedUsers.Values.Where(x => x.User.CanUserPlanetWars()), GenerateLobbyCommand());
            SaveStateToDb();
        }

        private Task UpdateLobby(string player)
        {
            return server.ConnectedUsers.Get(player)?.SendCommand(GenerateLobbyCommand());
        }

        private void SaveStateToDb()
        {
            using (var db = new ZkDataContext())
            {
                var gal = db.Galaxies.First(x => x.IsDefault);
                gal.MatchMakerState = JsonConvert.SerializeObject((PlanetWarsMatchMakerState)this);
                gal.AttackerSideCounter = AttackerSideCounter;
                gal.AttackerSideChangeTime = AttackerSideChangeTime;
                db.SaveChanges();
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


        // ===================== NESTED TYPES =====================

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
                return new PwMatchCommand.VoteOption
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
            }
        }
    }
}
