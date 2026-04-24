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
using Z.EntityFramework.Plus;
using ZkData;
using ZkLobbyServer;

namespace ZeroKWeb
{
    /// <summary>
    ///     Handles arranging and starting of PW games.
    ///     Parallel-turn model: every faction has its own list of attack options per cycle,
    ///     and each (PlanetID, AttackerFactionID) pair is an independent matchmaking slot with
    ///     its own attacker volunteers, defender volunteers, eligibility, and battle.
    /// </summary>
    public class PlanetWarsMatchMaker : PlanetWarsMatchMakerState
    {
        private readonly List<Faction> factions;

        private ZkLobbyServer.ZkLobbyServer server;
        private DateTime? defendersFullTime; // set when every formed squad has enough defender volunteers

        private Timer timer;

        public PlanetWarsMatchMaker(ZkLobbyServer.ZkLobbyServer server)
        {
            this.server = server;
            AttackOptions = new List<AttackOption>();
            FormedSquads = new List<AttackOption>();
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
                AttackOptions = dbState.AttackOptions ?? new List<AttackOption>();
                Phase = dbState.Phase;
                PhaseStartTime = dbState.PhaseStartTime;
                FormedSquads = dbState.FormedSquads ?? new List<AttackOption>();
                RunningBattles = dbState.RunningBattles ?? new Dictionary<int, AttackOption>();

                // sanity: if PhaseStartTime is in the future or too old, reset to now
                if (PhaseStartTime > DateTime.UtcNow || PhaseStartTime < DateTime.UtcNow.AddHours(-1))
                    PhaseStartTime = DateTime.UtcNow;
            }
            else
            {
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
                                Phase = PwPhase.DefendCollect;
                                PhaseStartTime = DateTime.UtcNow;
                                UpdateLobby();
                            }
                            else
                            {
                                // no attacks from any faction this cycle: restart cycle
                                ResetAttackOptions();
                            }
                        }
                        break;

                    case PwPhase.DefendCollect:
                        UpdateDefendersFullTime();

                        if (DateTime.UtcNow > GetEffectiveDefendDeadline())
                        {
                            // Guarantee state-machine progress: if any step throws (RunDefenderAssignment opens
                            // DB contexts, LaunchAllBattles interacts with Spring, etc.), we still reset and move
                            // to the next cycle. Otherwise a faulting tick would leave Phase stuck in DefendCollect
                            // past the deadline forever, re-throwing every second.
                            defendersFullTime = null;
                            try
                            {
                                RunDefenderAssignment();
                                await LaunchAllBattles();
                                RunGalaxyTick();
                                await ApplyTurnEndChargeBump();
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("PlanetWars cycle-end error: {0}", ex);
                            }
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

            // drop disconnected volunteers per option
            foreach (var opt in AttackOptions)
                opt.Attackers = opt.Attackers.Where(x => server.ConnectedUsers.ContainsKey(x)).ToList();

            // group options by attacker faction — each faction runs its own piercing pass
            foreach (var factionGroup in AttackOptions.GroupBy(o => o.AttackerFactionID))
            {
                if (factionGroup.Key == null) continue;
                FormSquadsForFaction(factionGroup.Key.Value, factionGroup.ToList());
            }

            AttackOptions.Clear();

            // notify attackers
            foreach (var squad in FormedSquads)
                server.Broadcast(squad.Attackers, new PwAttackingPlanet()
                {
                    PlanetID = squad.PlanetID,
                    AttackerFaction = GetFactionShortcut(squad.AttackerFactionID),
                });
        }

        private string GetFactionShortcut(int? factionId)
        {
            return factions.FirstOrDefault(f => f.FactionID == factionId)?.Shortcut;
        }

        private void FormSquadsForFaction(int attackerFactionId, List<AttackOption> factionOptions)
        {
            var playerOption = new Dictionary<string, AttackOption>(); // player -> their chosen option
            foreach (var opt in factionOptions)
                foreach (var name in opt.Attackers)
                    playerOption[name] = opt;

            if (!playerOption.Any()) return;

            // look up PW-WHR and PW-Rank for each player (role is faction-specific)
            var playerWhr = new Dictionary<string, double>();
            var playerRoleOrder = new Dictionary<string, int>();
            using (var db = new ZkDataContext())
            {
                foreach (var name in playerOption.Keys.ToList())
                {
                    var user = server.ConnectedUsers.Get(name)?.User;
                    if (user == null) { playerOption.Remove(name); continue; }

                    playerWhr[name] = GetPlayerWhr(name);

                    var account = db.Accounts.Find(user.AccountID);
                    var factionRole = account?.AccountRolesByAccountID
                        .Where(r => r.RoleType != null && !r.RoleType.IsClanOnly && r.RoleType.RestrictFactionID == attackerFactionId)
                        .Select(r => r.RoleType.DisplayOrder)
                        .OrderBy(x => x)
                        .Cast<int?>()
                        .FirstOrDefault();
                    playerRoleOrder[name] = factionRole ?? int.MaxValue;
                }
            }

            var pool = new HashSet<string>(playerOption.Keys);

            // Pass 1: form squads from any planet with >= TeamSize volunteers, top-WHR first
            bool formed;
            do
            {
                formed = false;
                foreach (var opt in factionOptions)
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
            } while (formed);

            // Pass 2: piercing — top PW-Rank player pulls others to their chosen planet
            while (pool.Count > 0)
            {
                var leader = pool
                    .OrderBy(x => playerRoleOrder.GetOrDefault(x, int.MaxValue))
                    .ThenByDescending(x => playerWhr.Get(x))
                    .First();

                var leaderOption = playerOption[leader];
                if (pool.Count < leaderOption.TeamSize) break;

                var fillers = pool
                    .Where(x => x != leader)
                    .OrderByDescending(x => playerWhr.Get(x))
                    .Take(leaderOption.TeamSize - 1)
                    .ToList();

                if (fillers.Count < leaderOption.TeamSize - 1) break;

                var squad = CreateSquadFromOption(leaderOption);
                squad.Attackers = new List<string> { leader };
                squad.Attackers.AddRange(fillers);
                FormedSquads.Add(squad);

                pool.Remove(leader);
                foreach (var p in fillers) pool.Remove(p);
            }

            // Pass 3: absorb leftovers into an existing squad on their original planet
            foreach (var name in pool.ToList())
            {
                var originalOption = playerOption[name];
                var squad = FormedSquads.FirstOrDefault(s => s.PlanetID == originalOption.PlanetID && s.AttackerFactionID == attackerFactionId);
                if (squad != null)
                {
                    squad.Attackers.Add(name);
                    squad.TeamSize = squad.Attackers.Count;
                    pool.Remove(name);
                }
            }
        }

        private AttackOption CreateSquadFromOption(AttackOption source)
        {
            return new AttackOption
            {
                PlanetID = source.PlanetID,
                Map = source.Map,
                Name = source.Name,
                OwnerFactionID = source.OwnerFactionID,
                AttackerFactionID = source.AttackerFactionID,
                TeamSize = source.TeamSize,
                PlanetImage = source.PlanetImage,
                IconSize = source.IconSize,
                StructureImages = source.StructureImages,
                Attackers = new List<string>(),
                Defenders = new List<string>(),
                DefenderVotes = new List<string>()
            };
        }


        // ===================== DEFENDER ASSIGNMENT =====================

        private void RunDefenderAssignment()
        {
            // collect all defender WHRs upfront
            var defenderWhr = new Dictionary<string, double>();
            foreach (var squad in FormedSquads)
            {
                foreach (var name in squad.DefenderVotes)
                {
                    if (defenderWhr.ContainsKey(name)) continue;
                    if (!server.ConnectedUsers.ContainsKey(name)) continue;
                    defenderWhr[name] = GetPlayerWhr(name);
                }
            }

            // each squad gets its direct volunteers first (top-WHR); overflow goes into a floating pool
            var floatingPool = new List<string>();
            foreach (var squad in FormedSquads)
            {
                var volunteers = squad.DefenderVotes
                    .Where(x => server.ConnectedUsers.ContainsKey(x) && defenderWhr.ContainsKey(x))
                    .OrderByDescending(x => defenderWhr[x])
                    .ToList();

                if (volunteers.Count > squad.TeamSize)
                {
                    squad.Defenders = volunteers.Take(squad.TeamSize).ToList();
                    floatingPool.AddRange(volunteers.Skip(squad.TeamSize));
                }
                else
                {
                    squad.Defenders = volunteers;
                }
            }

            // floating pool fills deficit on other squads where the defender's faction is eligible
            floatingPool = floatingPool.OrderByDescending(x => defenderWhr.Get(x)).Distinct().ToList();

            // cache per-squad defending factions and floating-pool faction IDs
            var squadDefendingFactions = new Dictionary<AttackOption, HashSet<int>>();
            foreach (var squad in FormedSquads)
                squadDefendingFactions[squad] = GetDefendingFactions(squad).Select(f => f.FactionID).ToHashSet();

            var defenderFactionId = new Dictionary<string, int?>();
            using (var db = new ZkDataContext())
            {
                foreach (var name in floatingPool)
                {
                    var user = server.ConnectedUsers.Get(name)?.User;
                    if (user != null) defenderFactionId[name] = db.Accounts.Find(user.AccountID)?.FactionID;
                }
            }

            // fill deficits: prioritize squads by attacker strength (strongest attacker squads get top floaters first)
            var squadsByAttackerStrength = FormedSquads
                .OrderByDescending(s => s.Attackers.Any() ? s.Attackers.Average(a => GetPlayerWhr(a)) : 0.0)
                .ToList();

            foreach (var squad in squadsByAttackerStrength)
            {
                var deficit = squad.TeamSize - squad.Defenders.Count;
                if (deficit <= 0 || floatingPool.Count == 0) continue;

                var allowedFactions = squadDefendingFactions[squad];
                var eligible = floatingPool
                    .Where(x => defenderFactionId.ContainsKey(x) && defenderFactionId[x].HasValue && allowedFactions.Contains(defenderFactionId[x].Value))
                    .Take(deficit)
                    .ToList();

                squad.Defenders.AddRange(eligible);
                foreach (var p in eligible) floatingPool.Remove(p);
            }
        }

        private double GetPlayerWhr(string name)
        {
            var user = server.ConnectedUsers.Get(name)?.User;
            if (user == null) return 0;
            return RatingSystems.GetRatingSystem(RatingCategory.Planetwars).GetPlayerRating(user.AccountID).LadderElo;
        }

        /// <summary>
        /// Average PW-WHR of the projected top-N squad out of a name pool, trimmed to at most <paramref name="slots"/>
        /// players. Returns 0 if pool is empty.
        /// </summary>
        private int AvgTopNWhr(IEnumerable<string> names, int slots)
        {
            if (slots <= 0) return 0;
            var whrs = names.Select(GetPlayerWhr).Where(w => w > 0).OrderByDescending(w => w).Take(slots).ToList();
            if (whrs.Count == 0) return 0;
            return (int)Math.Round(whrs.Average());
        }

        /// <summary>
        /// Standard Elo-logistic expected score, in percent 0-100.
        /// </summary>
        private static int? ComputeWinChance(int attackerAvg, int? defenderAvg)
        {
            if (attackerAvg <= 0 || defenderAvg == null || defenderAvg <= 0) return null;
            var chance = 1.0 / (1.0 + Math.Pow(10.0, (defenderAvg.Value - attackerAvg) / 400.0));
            return (int)Math.Round(chance * 100);
        }


        // ===================== LAUNCH BATTLES =====================

        private async Task LaunchAllBattles()
        {
            // charges are spent only for attackers whose squad reached a confirmed outcome — either
            // the Spring battle successfully started, or it was a concede. Battles that fail to start
            // (StartGame returned false, or setup threw) refund the commitment.
            var attackerNamesToChargeSpend = new List<string>();

            // one battle per squad — no merging across attacker factions
            foreach (var squad in FormedSquads.ToList())
            {
                squad.Attackers = squad.Attackers.Where(x => server.ConnectedUsers.ContainsKey(x)).ToList();
                squad.Defenders = squad.Defenders.Where(x => server.ConnectedUsers.ContainsKey(x)).ToList();

                if (squad.Attackers.Count == 0) continue;

                if (squad.Defenders.Count > 0)
                {
                    try
                    {
                        squad.TeamSize = Math.Max(squad.Attackers.Count, squad.Defenders.Count);
                        var battle = new PlanetWarsServerBattle(server, squad);
                        await server.AddBattle(battle);
                        RunningBattles[battle.BattleID] = squad;

                        foreach (var usr in squad.Attackers.Union(squad.Defenders))
                            await server.ForceJoinBattle(usr, battle);

                        if (await battle.StartGame())
                        {
                            attackerNamesToChargeSpend.AddRange(squad.Attackers);
                            var attackerFactionShortcut = GetFactionShortcut(squad.AttackerFactionID) ?? "?";
                            var text = $"Battle for planet {squad.Name} ({attackerFactionShortcut} attacks) starts on zk://@join_player:{squad.Attackers.FirstOrDefault()}  Roster: {string.Join(",", squad.Attackers)} vs {string.Join(",", squad.Defenders)}";
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
                else
                {
                    // concede — zero defenders. Attackers still "attacked", so charge applies.
                    attackerNamesToChargeSpend.AddRange(squad.Attackers);
                    RecordPlanetwarsLoss(squad);
                }
            }

            FormedSquads.Clear();

            if (attackerNamesToChargeSpend.Count > 0) await SpendAttackCharges(attackerNamesToChargeSpend);
        }

        private async Task SpendAttackCharges(List<string> playerNames)
        {
            var max = DynamicConfig.Instance.PwAttackChargesMax;
            if (max <= 0) return;
            try
            {
                List<Account> accounts;
                using (var db = new ZkDataContext())
                {
                    var turn = db.Galaxies.First(g => g.IsDefault).Turn;
                    accounts = db.Accounts.Where(a => playerNames.Contains(a.Name)).ToList();
                    foreach (var acc in accounts) acc.SpendPwAttackCharge(turn);
                    db.SaveChanges();
                }
                await Task.WhenAll(accounts.Select(acc => SendPwAttackCharges(server, acc.Name, acc)));
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars SpendAttackCharges error: {0}", ex);
            }
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
                if (conus.User.CanUserPlanetWars() && args.PlanetID > 0)
                    await JoinPlanet(conus.Name, args.PlanetID, args.AttackerFaction);
            }
        }

        public async Task OnCancel(ConnectedUser conus)
        {
            if (MiscVar.PlanetWarsMode == PlanetWarsModes.Running)
            {
                if (conus.User.CanUserPlanetWars()) await CancelPlanet(conus.Name);
            }
        }

        private async Task JoinPlanet(string userName, int planetId, string attackerFactionShortcut)
        {
            try
            {
                var user = server.ConnectedUsers.Get(userName)?.User;
                if (user == null) return;

                var faction = factions.FirstOrDefault(x => x.Shortcut == user.Faction);
                if (faction == null) return;

                if (Phase == PwPhase.AttackCollect)
                    await JoinPlanetAttack(userName, planetId, attackerFactionShortcut);
                else if (Phase == PwPhase.DefendCollect)
                    await JoinPlanetDefense(userName, planetId, attackerFactionShortcut);
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars {0} {1} {2} : {3}", nameof(JoinPlanet), userName, planetId, ex);
            }
        }

        private async Task JoinPlanetAttack(string userName, int targetPlanetId, string attackerFactionShortcut)
        {
            var conus = server.ConnectedUsers.Get(userName);
            var user = conus?.User;
            if (user == null) return;

            // AttackerFaction is mandatory and must match the user's own faction — attackers can only attack
            // for themselves. A mismatch or missing value indicates a client bug or tampering; reject silently.
            if (string.IsNullOrEmpty(attackerFactionShortcut) || attackerFactionShortcut != user.Faction) return;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.Find(user.AccountID);
                if (account == null || account.FactionID == null || !account.CanPlayerPlanetWars()) return;

                var attackOption = AttackOptions.Find(x => x.PlanetID == targetPlanetId && x.AttackerFactionID == account.FactionID);
                if (attackOption == null) return;

                var maxCharges = DynamicConfig.Instance.PwAttackChargesMax;
                if (maxCharges > 0 && account.PwAttackCharges <= 0)
                {
                    await server.GhostChanSay(user.Faction, $"{userName} cannot attack: out of attack charges");
                    return;
                }

                // remove from other attack options (same faction only — other factions' options are independent)
                foreach (var aop in AttackOptions.Where(x => x.AttackerFactionID == account.FactionID && x.PlanetID != targetPlanetId))
                    aop.Attackers.RemoveAll(x => x == userName);

                if (!attackOption.Attackers.Contains(userName))
                {
                    attackOption.Attackers.Add(user.Name);
                    await server.GhostChanSay(user.Faction, $"{userName} joins attack on {attackOption.Name}");
                    await conus.SendCommand(new PwJoinPlanetSuccess()
                    {
                        PlanetID = targetPlanetId,
                        AttackerFaction = GetFactionShortcut(attackOption.AttackerFactionID),
                    });
                    await UpdateLobby();
                }
            }
        }

        private async Task JoinPlanetDefense(string userName, int targetPlanetId, string attackerFactionShortcut)
        {
            var conus = server.ConnectedUsers.Get(userName);
            var user = conus?.User;
            if (user == null) return;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.Find(user.AccountID);
                if (account == null || !account.CanPlayerPlanetWars()) return;

                if (string.IsNullOrEmpty(attackerFactionShortcut)) return;
                var attackerFaction = factions.FirstOrDefault(f => f.Shortcut == attackerFactionShortcut);
                if (attackerFaction == null) return;
                var squad = FormedSquads.FirstOrDefault(s => s.PlanetID == targetPlanetId && s.AttackerFactionID == attackerFaction.FactionID);
                if (squad == null) return;

                // attack vs defend are mutually exclusive per cycle. A player already locked into a squad's attack
                // cannot also defend — otherwise LaunchAllBattles would force-join them into two Spring battles.
                if (FormedSquads.Any(s => s.Attackers.Contains(userName)))
                {
                    await server.GhostChanSay(user.Faction, $"{userName} cannot defend — already committed as attacker this cycle");
                    return;
                }

                // player's faction must be in the squad's defending factions
                var defendingFactions = GetDefendingFactions(squad);
                if (!defendingFactions.Any(f => f.FactionID == account.FactionID))
                {
                    await server.GhostChanSay(user.Faction, $"{userName} cannot defend {squad.Name} (not owner or allied)");
                    return;
                }

                // remove from all other defender lists (locked to one defense per cycle)
                foreach (var s in FormedSquads) s.DefenderVotes.RemoveAll(x => x == userName);

                if (!squad.DefenderVotes.Contains(userName))
                {
                    squad.DefenderVotes.Add(userName);
                    UpdateDefendersFullTime();
                    await server.GhostChanSay(user.Faction, $"{userName} joins defense of {squad.Name}");
                    await conus.SendCommand(new PwJoinPlanetSuccess()
                    {
                        PlanetID = targetPlanetId,
                        AttackerFaction = GetFactionShortcut(squad.AttackerFactionID),
                    });
                    await UpdateLobby();
                }
            }
        }

        /// <summary>
        /// Clear the player's attack or defense commitment for the current cycle.
        /// Works in both phases.
        /// </summary>
        private async Task CancelPlanet(string userName)
        {
            bool changed = false;

            if (Phase == PwPhase.AttackCollect)
            {
                foreach (var opt in AttackOptions)
                    changed |= opt.Attackers.RemoveAll(x => x == userName) > 0;
            }
            else if (Phase == PwPhase.DefendCollect)
            {
                foreach (var s in FormedSquads)
                    changed |= s.DefenderVotes.RemoveAll(x => x == userName) > 0;
                if (changed) UpdateDefendersFullTime();
            }

            if (changed)
            {
                var conus = server.ConnectedUsers.Get(userName);
                if (conus?.User != null) await server.GhostChanSay(conus.User.Faction, $"{userName} cancelled their pick");
                await UpdateLobby();
            }
        }


        // ===================== CONNECTION EVENTS =====================

        public async Task OnLoginAccepted(ConnectedUser connectedUser)
        {
            await connectedUser.SendCommand(GeneratePwStatus());

            if (MiscVar.PlanetWarsMode == PlanetWarsModes.Running)
            {
                var u = connectedUser.User;
                if (u.CanUserPlanetWars())
                {
                    await connectedUser.SendCommand(GenerateLobbyCommand(u.Name, u.Faction));
                    await SendPwAttackChargesForUser(u.Name);
                }
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
                    foreach (var squad in FormedSquads)
                    {
                        changed |= squad.DefenderVotes.RemoveAll(x => x == name) > 0;
                        changed |= squad.Attackers.RemoveAll(x => x == name) > 0;
                    }
                    if (changed) UpdateDefendersFullTime();
                }

                if (changed) await UpdateLobby();
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars OnUserDisconnected: {0}", ex);
            }
        }


        // ===================== LOBBY COMMANDS =====================

        /// <summary>
        /// Per-option data that does not depend on the viewer. Built once per lobby-update fan-out and reused
        /// across viewers — the hot path (UpdateLobby) would otherwise recompute WHR averages, open DB contexts
        /// inside GetDefendingFactions, and re-encode keys for every connected PW user.
        /// </summary>
        private sealed class OptionSnapshot
        {
            public int PlanetId;
            public int? AttackerFactionId;
            public string AttackerFactionShortcut;
            public string PlanetName;
            public string Map;
            public int IconSize;
            public List<string> StructureImages;
            public string PlanetImage;
            public int Count;
            public int Needed;
            public int AttackerAvgWhr;
            public int? DefenderAvgWhr;
            public int? WinChance;
            public HashSet<string> AttackerNames;
            public HashSet<string> DefenderNames;
            public HashSet<int> DefenderFactionIds; // DefendCollect only
        }

        private List<OptionSnapshot> ComputeOptionSnapshots(PwPhase phase)
        {
            var result = new List<OptionSnapshot>();
            if (phase == PwPhase.AttackCollect)
            {
                foreach (var opt in AttackOptions)
                {
                    result.Add(new OptionSnapshot
                    {
                        PlanetId = opt.PlanetID,
                        AttackerFactionId = opt.AttackerFactionID,
                        AttackerFactionShortcut = GetFactionShortcut(opt.AttackerFactionID),
                        PlanetName = opt.Name,
                        Map = opt.Map,
                        IconSize = opt.IconSize,
                        StructureImages = opt.StructureImages,
                        PlanetImage = opt.PlanetImage,
                        Count = opt.Attackers.Count,
                        Needed = opt.TeamSize,
                        AttackerAvgWhr = AvgTopNWhr(opt.Attackers, opt.TeamSize),
                        DefenderAvgWhr = null,
                        WinChance = null,
                        AttackerNames = new HashSet<string>(opt.Attackers),
                        DefenderNames = new HashSet<string>(),
                        DefenderFactionIds = null,
                    });
                }
            }
            else if (phase == PwPhase.DefendCollect)
            {
                foreach (var squad in FormedSquads)
                {
                    var atkAvg = AvgTopNWhr(squad.Attackers, squad.TeamSize);
                    int? defAvg = squad.DefenderVotes.Count > 0 ? (int?)AvgTopNWhr(squad.DefenderVotes, squad.TeamSize) : null;
                    result.Add(new OptionSnapshot
                    {
                        PlanetId = squad.PlanetID,
                        AttackerFactionId = squad.AttackerFactionID,
                        AttackerFactionShortcut = GetFactionShortcut(squad.AttackerFactionID),
                        PlanetName = squad.Name,
                        Map = squad.Map,
                        IconSize = squad.IconSize,
                        StructureImages = squad.StructureImages,
                        PlanetImage = squad.PlanetImage,
                        Count = squad.DefenderVotes.Count,
                        Needed = squad.TeamSize,
                        AttackerAvgWhr = atkAvg,
                        DefenderAvgWhr = defAvg,
                        WinChance = ComputeWinChance(atkAvg, defAvg),
                        AttackerNames = new HashSet<string>(squad.Attackers),
                        DefenderNames = new HashSet<string>(squad.DefenderVotes),
                        DefenderFactionIds = GetDefendingFactions(squad).Select(f => f.FactionID).ToHashSet(),
                    });
                }
            }
            return result;
        }

        public PwMatchCommand GenerateLobbyCommand(string playerName = null, string playerFaction = null)
        {
            if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running)
                return new PwMatchCommand(PwMatchCommand.ModeType.Clear);
            return StampLobbyCommand(ComputeOptionSnapshots(Phase), Phase, playerName, playerFaction);
        }

        private PwMatchCommand StampLobbyCommand(List<OptionSnapshot> snapshots, PwPhase phase, string playerName, string playerFaction)
        {
            try
            {
                int? playerFactionId = null;
                if (playerFaction != null)
                    playerFactionId = factions.FirstOrDefault(f => f.Shortcut == playerFaction)?.FactionID;

                if (phase == PwPhase.AttackCollect)
                {
                    // All factions' options are shown to every viewer (parity with pre-parallel-turn UX, where
                    // everyone could see what the current attacker was planning). CanSelectForBattle gates the
                    // click: a player can only join options for their own faction.
                    var options = snapshots.Select(s => new PwMatchCommand.VoteOption
                    {
                        PlanetID = s.PlanetId,
                        PlanetName = s.PlanetName,
                        Map = s.Map,
                        IconSize = s.IconSize,
                        StructureImages = s.StructureImages,
                        PlanetImage = s.PlanetImage,
                        Count = s.Count,
                        Needed = s.Needed,
                        CanSelectForBattle = playerFactionId != null && playerFactionId == s.AttackerFactionId,
                        PlayerIsAttacker = playerName != null && s.AttackerNames.Contains(playerName),
                        PlayerIsDefender = false,
                        AttackerFaction = s.AttackerFactionShortcut,
                        AttackerAvgWhr = s.AttackerAvgWhr,
                        DefenderAvgWhr = null,
                        WinChance = null,
                    }).ToList();

                    var deadline = GetAttackDeadline();
                    return new PwMatchCommand(PwMatchCommand.ModeType.Attack)
                    {
                        Options = options,
                        Deadline = deadline,
                        DeadlineSeconds = (int)deadline.Subtract(DateTime.UtcNow).TotalSeconds,
                        AttackerFaction = playerFaction,
                    };
                }
                else // DefendCollect
                {
                    var allDefenderShortcuts = new HashSet<string>();
                    var options = new List<PwMatchCommand.VoteOption>(snapshots.Count);
                    foreach (var s in snapshots)
                    {
                        var playerIsAttacker = playerName != null && s.AttackerNames.Contains(playerName);
                        var canDefend = playerFactionId != null && s.DefenderFactionIds != null && s.DefenderFactionIds.Contains(playerFactionId.Value);
                        options.Add(new PwMatchCommand.VoteOption
                        {
                            PlanetID = s.PlanetId,
                            PlanetName = s.PlanetName,
                            Map = s.Map,
                            IconSize = s.IconSize,
                            StructureImages = s.StructureImages,
                            PlanetImage = s.PlanetImage,
                            Count = s.Count,
                            Needed = s.Needed,
                            CanSelectForBattle = canDefend && !playerIsAttacker,
                            PlayerIsAttacker = playerIsAttacker,
                            PlayerIsDefender = playerName != null && s.DefenderNames.Contains(playerName),
                            AttackerFaction = s.AttackerFactionShortcut,
                            AttackerAvgWhr = s.AttackerAvgWhr,
                            DefenderAvgWhr = s.DefenderAvgWhr,
                            WinChance = s.WinChance,
                        });
                        if (s.DefenderFactionIds != null)
                            foreach (var fid in s.DefenderFactionIds)
                            {
                                var sc = GetFactionShortcut(fid);
                                if (sc != null) allDefenderShortcuts.Add(sc);
                            }
                    }

                    var deadline = GetEffectiveDefendDeadline();
                    return new PwMatchCommand(PwMatchCommand.ModeType.Defend)
                    {
                        Options = options,
                        Deadline = deadline,
                        DeadlineSeconds = (int)deadline.Subtract(DateTime.UtcNow).TotalSeconds,
                        AttackerFaction = null,
                        DefenderFactions = allDefenderShortcuts.ToList(),
                    };
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars {0}: {1}", nameof(StampLobbyCommand), ex);
                return null;
            }
        }


        // ===================== ATTACK OPTIONS =====================

        /// <summary>
        ///     Invoked from the web page — adds a planet as an attack option for the specified attacker faction.
        ///     Each (PlanetID, AttackerFactionID) is an independent slot.
        /// </summary>
        public void AddAttackOption(Planet planet, int attackerFactionId)
        {
            try
            {
                if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running) return;
                if (Phase != PwPhase.AttackCollect) return;
                if (planet.OwnerFactionID == attackerFactionId) return;
                if (AttackOptions.Any(x => x.PlanetID == planet.PlanetID && x.AttackerFactionID == attackerFactionId)) return;

                using (var db = new ZkDataContext())
                {
                    var attackerFaction = db.Factions.Find(attackerFactionId);
                    if (attackerFaction == null || !planet.CanMatchMakerPlay(attackerFaction)) return;
                }

                InternalAddOption(planet, attackerFactionId);
                UpdateLobby();
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
            Phase = PwPhase.AttackCollect;
            PhaseStartTime = DateTime.UtcNow;

            // TODO re-enable to prevent attacking planets with running battles
            // var contestedPlanetIds = RunningBattles.Values.Select(x => x.PlanetID).ToHashSet();
            var contestedPlanetIds = new HashSet<int>();
            var perFactionCount = DynamicConfig.Instance.PwAttackOptionCount;

            using (var db = new ZkDataContext())
            {
                var gal = db.Galaxies.First(x => x.IsDefault);
                var allPlanets = gal.Planets.ToList();

                foreach (var attackerFaction in factions)
                {
                    var attacker = db.Factions.Find(attackerFaction.FactionID);
                    if (attacker == null) continue;

                    var sorted = allPlanets
                        .Where(x => x.OwnerFactionID != attackerFaction.FactionID)
                        .OrderByDescending(x => x.PlanetFactions.Where(y => y.FactionID == attackerFaction.FactionID).Sum(y => y.Dropships))
                        .ThenByDescending(x => x.PlanetFactions.Where(y => y.FactionID == attackerFaction.FactionID).Sum(y => y.Influence))
                        .ToList();

                    int cnt = perFactionCount;
                    foreach (var planet in sorted)
                    {
                        if (cnt == 0) break;
                        if (!planet.CanMatchMakerPlay(attacker)) continue;
                        if (contestedPlanetIds.Contains(planet.PlanetID)) continue;
                        InternalAddOption(planet, attackerFaction.FactionID);
                        cnt--;
                    }

                    // ensure at least one TeamSize=2 option (easy-to-fill squad)
                    if (!AttackOptions.Any(y => y.AttackerFactionID == attackerFaction.FactionID && y.TeamSize == 2))
                    {
                        var planet = sorted.FirstOrDefault(x => x.TeamSize == 2 && x.CanMatchMakerPlay(attacker) && !contestedPlanetIds.Contains(x.PlanetID));
                        if (planet != null) InternalAddOption(planet, attackerFaction.FactionID);
                    }
                }
            }

            UpdateLobby();
            foreach (var fac in factions)
                server.GhostChanSay(fac.Shortcut, "New PlanetWars cycle — select a planet to attack or defend");
        }

        private void InternalAddOption(Planet planet, int attackerFactionId)
        {
            AttackOptions.Add(new AttackOption
            {
                PlanetID = planet.PlanetID,
                Map = planet.Resource.InternalName,
                OwnerFactionID = planet.OwnerFactionID,
                AttackerFactionID = attackerFactionId,
                Name = planet.Name,
                TeamSize = planet.TeamSize,
                PlanetImage = planet.Resource?.MapPlanetWarsIcon,
                IconSize = planet.Resource?.PlanetWarsIconSize ?? 0,
                StructureImages = planet.PlanetStructures.Select(x => x.IsActive ? x.StructureType.MapIcon : x.StructureType.DisabledMapIcon).ToList()
            });
        }


        // ===================== HELPERS =====================

        /// <summary>
        /// Factions allowed to defend the given squad (i.e. versus the squad's attacker faction).
        /// Owner always defends; allies with EffectBalanceSameSide treaty vs. THIS specific attacker also defend.
        /// </summary>
        public List<Faction> GetDefendingFactions(AttackOption target)
        {
            if (target.OwnerFactionID != null)
            {
                var ret = new List<Faction>();
                var owner = factions.Find(x => x.FactionID == target.OwnerFactionID);
                if (owner != null) ret.Add(owner);

                using (var db = new ZkDataContext())
                {
                    var planet = db.Planets.Find(target.PlanetID);
                    if (planet != null)
                    {
                        foreach (var of in db.Factions.Where(x => !x.IsDeleted && x.FactionID != target.OwnerFactionID && x.FactionID != target.AttackerFactionID))
                        {
                            if (of.GaveTreatyRight(planet, x => x.EffectBalanceSameSide == true))
                            {
                                var match = factions.FirstOrDefault(x => x.FactionID == of.FactionID);
                                if (match != null) ret.Add(match);
                            }
                        }
                    }
                }
                return ret;
            }

            // no owner — anyone but the attacker may defend
            return factions.Where(x => x.FactionID != target.AttackerFactionID).ToList();
        }

        private void RecordPlanetwarsLoss(AttackOption option)
        {
            var attackerFaction = factions.FirstOrDefault(f => f.FactionID == option.AttackerFactionID);
            var attackerName = attackerFaction?.Name ?? "Attacker";
            var message = $"{attackerName} won {option.Name} because nobody tried to defend";
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

        /// <summary>
        /// Effective defend deadline accounting for the 30s early cutoff when all squads are fully defended.
        /// </summary>
        private DateTime GetEffectiveDefendDeadline()
        {
            var deadline = GetDefendDeadline();
            if (defendersFullTime != null)
            {
                var earlyDeadline = defendersFullTime.Value.AddSeconds(30);
                if (earlyDeadline < deadline) deadline = earlyDeadline;
            }
            return deadline;
        }

        /// <summary>
        /// Each squad must independently have volunteers >= its slots.
        /// </summary>
        private void UpdateDefendersFullTime()
        {
            var allFull = FormedSquads.Any();
            foreach (var squad in FormedSquads)
            {
                if (squad.DefenderVotes.Count < squad.TeamSize) { allFull = false; break; }
            }

            if (allFull)
            {
                if (defendersFullTime == null) defendersFullTime = DateTime.UtcNow;
            }
            else
            {
                defendersFullTime = null;
            }
        }

        public void RemoveFromRunningBattles(int battleID)
        {
            RunningBattles.Remove(battleID);
        }


        // ===================== ATTACK CHARGES =====================

        public static PwAttackCharges BuildPwAttackCharges(Account account)
        {
            var max = DynamicConfig.Instance.PwAttackChargesMax;
            int? nextRechargeTurn = null;
            if (max > 0 && account.PwAttackCharges < max && account.PwLastChargeChangeTurn != null)
                nextRechargeTurn = account.PwLastChargeChangeTurn.Value + DynamicConfig.Instance.PwAttackChargesCooldownTurns;
            return new PwAttackCharges
            {
                Current = account.PwAttackCharges,
                NextRechargeTurn = nextRechargeTurn,
            };
        }

        public static async Task SendPwAttackCharges(ZkLobbyServer.ZkLobbyServer server, string userName, Account account)
        {
            var conus = server.ConnectedUsers.Get(userName);
            if (conus == null) return;
            await conus.SendCommand(BuildPwAttackCharges(account));
        }

        private async Task SendPwAttackChargesForUser(string userName)
        {
            var conus = server.ConnectedUsers.Get(userName);
            if (conus?.User == null) return;
            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.Find(conus.User.AccountID);
                if (account == null) return;
                await conus.SendCommand(BuildPwAttackCharges(account));
            }
        }

        private async Task ApplyTurnEndChargeBump()
        {
            try
            {
                var max = DynamicConfig.Instance.PwAttackChargesMax;
                if (max <= 0) return;
                var cooldown = DynamicConfig.Instance.PwAttackChargesCooldownTurns;

                List<Account> bumped;
                using (var db = new ZkDataContext())
                {
                    var turn = db.Galaxies.First(g => g.IsDefault).Turn;
                    bumped = db.Accounts.Where(a =>
                        a.FactionID != null &&
                        a.PwAttackCharges < max &&
                        (a.PwLastChargeChangeTurn == null || turn - a.PwLastChargeChangeTurn >= cooldown)).ToList();

                    foreach (var acc in bumped)
                    {
                        acc.PwAttackCharges++;
                        acc.PwLastChargeChangeTurn = turn;
                    }

                    if (bumped.Count > 0) db.SaveChanges();
                }

                await Task.WhenAll(bumped.Select(a => SendPwAttackCharges(server, a.Name, a)));
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlanetWars turn-end charge bump error: {0}", ex);
            }
        }

        private async Task UpdateLobby()
        {
            var users = server.ConnectedUsers.Values.Where(x => x.User.CanUserPlanetWars()).ToList();
            if (MiscVar.PlanetWarsMode != PlanetWarsModes.Running)
            {
                var clear = new PwMatchCommand(PwMatchCommand.ModeType.Clear);
                await Task.WhenAll(users.Select(u => u.SendCommand(clear)));
                SaveStateToDb();
                return;
            }

            // compute viewer-invariant data once, stamp per-viewer flags in parallel send fan-out
            var snapshots = ComputeOptionSnapshots(Phase);
            var phase = Phase;
            await Task.WhenAll(users.Select(u => u.SendCommand(StampLobbyCommand(snapshots, phase, u.Name, u.User.Faction))));
            SaveStateToDb();
        }

        private void SaveStateToDb()
        {
            using (var db = new ZkDataContext())
            {
                var gal = db.Galaxies.First(x => x.IsDefault);
                gal.MatchMakerState = JsonConvert.SerializeObject((PlanetWarsMatchMakerState)this);
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
            /// <summary>Sliced defender roster (populated by <see cref="RunDefenderAssignment"/> at end of DefendCollect).</summary>
            public List<string> Defenders { get; set; }
            /// <summary>Defender volunteers pre-slicing. Sliced into <see cref="Defenders"/> by <see cref="RunDefenderAssignment"/>.</summary>
            public List<string> DefenderVotes { get; set; }
            public string Map { get; set; }
            public string Name { get; set; }
            public int? OwnerFactionID { get; set; }
            /// <summary>Faction that will be attacking on this option — each (PlanetID, AttackerFactionID) is an independent slot.</summary>
            public int? AttackerFactionID { get; set; }
            public int PlanetID { get; set; }
            public int TeamSize { get; set; }
            public List<string> StructureImages { get; set; } = new List<string>();
            public int IconSize { get; set; }
            public string PlanetImage { get; set; }

            public AttackOption()
            {
                Attackers = new List<string>();
                Defenders = new List<string>();
                DefenderVotes = new List<string>();
            }
        }
    }
}
