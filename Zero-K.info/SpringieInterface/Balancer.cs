using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class BalanceTeamsResult
    {
        public List<BotTeam> Bots = new List<BotTeam>();
        public bool CanStart = true;
        public bool DeleteBots;
        public string Message;
        public List<PlayerTeam> Players = new List<PlayerTeam>();
    }

    public class Balancer
    {
        const double MaxCbalanceDifference = 150;
        const double MaxPwEloDifference = 200;
        readonly List<BalanceTeam> teams = new List<BalanceTeam>();
        List<BalanceItem> balanceItems;

        double bestStdDev = double.MaxValue;
        List<BalanceTeam> bestTeams;
        long iterationsChecked;
        int maxTeamSize;

        public static BalanceTeamsResult BalanceTeams(BattleContext context, bool isGameStart, int? allyCount, bool? clanWise) {
            AutohostConfig config = context.GetConfig();
            int playerCount = context.Players.Count(x => !x.IsSpectator);

            // game is managed, is bigger than split limit and wants to start -> split into two and issue starts
            if (isGameStart && context.GetMode() != AutohostMode.None && config.SplitBiggerThan != null && config.SplitBiggerThan < playerCount) {
                new Thread(() =>
                    {
                        try {
                            SplitAutohost(context);
                        } catch {}
                        ;
                    }).Start();
                return new BalanceTeamsResult
                       {
                           CanStart = false,
                           Message =
                               string.Format(
                                   "Game too big - splitting into two - max players is {0} here. Use !forcestart instead of !start to override.",
                                   config.SplitBiggerThan)
                       };
            }

            // dont allow to start alone
            if (context.Players.Count() <= 1 && !context.Bots.Any()) return new BalanceTeamsResult { CanStart = false, Message = "Cannot play alone, you can add bots using button on bottom left." };

            if (clanWise == null &&
                (config.AutohostMode == AutohostMode.BigTeams || config.AutohostMode == AutohostMode.SmallTeams ||
                 config.AutohostMode == AutohostMode.Experienced)) clanWise = true;

            BalanceTeamsResult res = PerformBalance(context, isGameStart, allyCount, clanWise, config, playerCount);

            if (!isGameStart) {
                if (playerCount < (config.MinToStart ?? 0)) {
                    //res.Message = string.Format("This host needs at least {0} people to start", config.MinToStart);
                    res.CanStart = false;
                    //return res;
                }
                else if (playerCount > (config.MaxToStart ?? 99) || playerCount > (config.SplitBiggerThan ?? 99)) {
                    //res.Message = string.Format("This host can only start with less than {0} people, wait for juggler to split you", Math.Min(config.MaxToStart??0, config.SplitBiggerThan??0));
                    res.CanStart = false;
                    //return res;
                }
                if (playerCount%2 == 1 && playerCount < 8) res.CanStart = false;
                if (config.AutohostMode == AutohostMode.Game1v1 || config.AutohostMode == AutohostMode.GameFFA ||
                    config.AutohostMode == AutohostMode.None) res.CanStart = false;
            }

            if (isGameStart) VerifySpecCheaters(context, res);

            return res;
        }

        public static List<BalanceTeam> CloneTeams(List<BalanceTeam> t) {
            return new List<BalanceTeam>(t.Select(x => x.Clone()));
        }

        public static double GetTeamsDifference(List<BalanceTeam> t) {
            if (t.Count == 2) return Math.Abs(t[0].AvgElo - t[1].AvgElo);
            double min = Double.MaxValue;
            double max = Double.MinValue;
            foreach (BalanceTeam team in t) {
                if (team.AvgElo > max) max = team.AvgElo;
                if (team.AvgElo < min) min = team.AvgElo;
            }
            return max - min;
        }

        BalanceTeamsResult PlanetwarsBalance(BattleContext context) {
            var res = new BalanceTeamsResult();
            context.Players = context.Players.Where(x => !x.IsSpectator).ToList();
            res.CanStart = false;
            res.DeleteBots = true;

            using (var db = new ZkDataContext()) {
                res.Message = "";
                Planet planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == context.Map);
                List<int> idList = context.Players.Select(x => x.LobbyID).ToList();
                List<Account> players = idList.Select(x => db.Accounts.First(y => y.LobbyID == x)).ToList();
                List<int> presentFactions = players.Where(x => x != null).GroupBy(x => x.FactionID??0).Select(x => x.Key).ToList();
                Faction attackerFaction = planet.GetAttacker(presentFactions);
                if (attackerFaction == null) {
                    res.Message = "No planet was attacked - send your dropships somewhere!";
                    return res;
                }
                Faction defenderFaction = planet.Faction;
                if (defenderFaction != null && !players.Any(x => x.Faction == defenderFaction && x.Clan != null)) {
                    res.Message = "Missing clanned defender of this planet";
                    return res;
                }

                // bots game
                if (planet.PlanetStructures.Any(x => !string.IsNullOrEmpty(x.StructureType.EffectBots))) {
                    int teamID = 0;
                    for (int i = 0; i < players.Count; i++) res.Players.Add(new PlayerTeam { LobbyID = players[i].LobbyID ?? 0, Name = players[i].Name, AllyID = 0, TeamID = teamID++ });
                    int cnt = 1;
                    foreach (StructureType b in planet.PlanetStructures.Select(x => x.StructureType).Where(x => !string.IsNullOrEmpty(x.EffectBots))) res.Bots.Add(new BotTeam { AllyID = 1, BotAI = b.EffectBots, TeamID = teamID++, BotName = "Aliens" + cnt++ });

                    res.Message += string.Format("This planet is infested by aliens, fight for your survival");
                    return res;
                }

                // create attacker and defenders teams
                List<Account> attackers = players.Where(x => x.Faction == attackerFaction).ToList();
                var defenders = new List<Account>();
                if (defenderFaction != null) defenders = players.Where(x => x.Faction == defenderFaction).ToList();
                foreach (Account acc in players) {
                    if (acc.Faction != null && acc.Faction != attackerFaction && acc.Faction != defenderFaction) {
                        bool allyAttacker = attackerFaction.HasTreatyRight(acc.Faction, x => x.EffectBalanceSameSide == true, planet);
                        bool allyDefender = defenderFaction != null &&
                                            defenderFaction.HasTreatyRight(acc.Faction, x => x.EffectBalanceSameSide == true, planet);
                        if (allyAttacker && allyDefender) continue;
                        if (allyAttacker) attackers.Add(acc);
                        if (allyDefender) defenders.Add(acc);
                    }
                }

                return LegacyBalance(2, true, context, attackers, defenders);
            }
        }


        BalanceTeamsResult LegacyBalance(int teamCount, bool clanwise, BattleContext b, params List<Account>[] unmovablePlayers) {
            var ret = new BalanceTeamsResult();

            try {
                
                ret.CanStart = true;
                ret.Players = b.Players.ToList();

                var db = new ZkDataContext();
                List<Account> accs =
                    db.Accounts.Where(x => b.Players.Where(y => !y.IsSpectator).Select(y => (int?)y.LobbyID).ToList().Contains(x.LobbyID)).ToList();
                if (accs.Count < 1) {
                    ret.CanStart = false;
                    return ret;
                }
                if (teamCount < 1) teamCount = 1;
                if (teamCount > accs.Count) teamCount = accs.Count;
                if (teamCount == 1) {
                    foreach (PlayerTeam p in ret.Players) p.AllyID = 0;
                    return ret;
                }

                maxTeamSize = (int)Math.Ceiling(accs.Count/(double)teamCount);

                teams.Clear();
                for (int i = 0; i < teamCount; i++) {
                    var team = new BalanceTeam();
                    teams.Add(team);
                    if (unmovablePlayers != null && unmovablePlayers.Length > i) {
                        List<Account> unmovables = unmovablePlayers[i];
                        team.AddItem(new BalanceItem(unmovablePlayers[i].ToArray()) { CanBeMoved = false });
                        accs.RemoveAll(x => unmovables.Any(y => y.LobbyID == x.LobbyID));
                    }
                }

                balanceItems = new List<BalanceItem>();
                if (clanwise) {
                    List<IGrouping<int?, Account>> clanGroups = accs.GroupBy(x => x.ClanID ?? x.LobbyID).ToList();
                    if (teamCount > clanGroups.Count() || clanGroups.Any(x => x.Count() > maxTeamSize)) clanwise = false;
                    else balanceItems.AddRange(clanGroups.Select(x => new BalanceItem(x.ToArray())));
                }
                if (!clanwise) {
                    balanceItems.Clear();
                    balanceItems.AddRange(accs.Select(x => new BalanceItem(x)));
                }

                var sw = new Stopwatch();
                sw.Start();
                RecursiveBalance(0);
                sw.Stop();

                if (clanwise && (bestTeams == null || GetTeamsDifference(bestTeams) > MaxCbalanceDifference)) return new Balancer().LegacyBalance(teamCount, false, b, unmovablePlayers); // cbalance failed, rebalance using normal

                var minSize = bestTeams.Min(x => x.Count);
                var maxSize = bestTeams.Max(x => x.Count);
                if (maxSize/(double)minSize > 2) {
                    if (clanwise) return new Balancer().LegacyBalance(teamCount, false, b, unmovablePlayers); // cbalance failed, rebalance using normal
                    
                    ret.CanStart = false;
                    ret.Message = string.Format("Failed to balance - too many people from same faction");
                    return ret;
                }

                if (unmovablePlayers != null && unmovablePlayers.Length> 0) {
                    var minElo = bestTeams.Min(x => x.AvgElo);
                    var maxElo = bestTeams.Max(x => x.AvgElo);
                    if (maxElo - minElo > MaxPwEloDifference) {
                        ret.CanStart = false;
                        ret.Message = string.Format("Team difference is too big - win chance {0}% - spectate some or wait for more people",
                                                    Utils.GetWinChancePercent(maxElo - minElo));
                        return ret;
                    }

                }

                if (bestTeams == null) {
                    ret.CanStart = false;
                    ret.Message = string.Format("Failed to balance {0}",
                                                (clanwise
                                                     ? "- too many people from same clan? Try !balance and !forcestart"
                                                     : ". Use !random and !forcestart"));
                    return ret;
                }
                else {
                    if (unmovablePlayers == null || unmovablePlayers.Length == 0) bestTeams = bestTeams.Shuffle(); // permute when not unmovable players present

                    string text = "(ratings ";

                    double lastTeamElo = 0.0;
                    int allyNum = 0;
                    foreach (BalanceTeam team in bestTeams) {
                        if (allyNum > 0) text += " : ";
                        text += string.Format("{0}={1}", (allyNum + 1), Math.Round(team.AvgElo));
                        if (allyNum > 0) text += string.Format(" ({0}%)", Utils.GetWinChancePercent(lastTeamElo - team.AvgElo));
                        lastTeamElo = team.AvgElo;

                        foreach (int u in team.Items.SelectMany(x => x.LobbyId)) ret.Players.Single(x => x.LobbyID == u).AllyID = allyNum;
                        allyNum++;
                    }
                    text += ")";

                    ret.Message = String.Format("{0} players balanced {2} to {1} teams {3}. {4} combinations checked, spent {5}ms of CPU time",
                                                bestTeams.Sum(x=>x.Count),
                                                teamCount,
                                                clanwise ? "respecting clans" : "",
                                                text,
                                                iterationsChecked,
                                                sw.ElapsedMilliseconds);
                }
            } catch (Exception ex) {
                ret.Message = ex.ToString();
                ret.CanStart = false;
            }

            return ret;
        }

        static void SpecPlayerOnCondition(PlayerTeam player, Account account, string userMessage) {
            player.IsSpectator = true;
            AuthServiceClient.SendLobbyMessage(account, userMessage);
        }

        static bool CheckPlayersMinimumConditions(BattleContext battleContext,
                                                  ZkDataContext dataContext,
                                                  AutohostConfig config,
                                                  ref string actionsDescription) {
            bool ok = true;
            foreach (var p in battleContext.Players.Where(x=>!x.IsSpectator).Select(x => new { player = x, account = dataContext.Accounts.First(y => y.LobbyID == x.LobbyID) })
                ) {
                if (config.MinLevel != null && p.account.Level < config.MinLevel) {
                    SpecPlayerOnCondition(p.player,
                                          p.account,
                                          string.Format(
                                              "Sorry, minimum level is {0} on this host. To increase your level, play more games on other hosts or open multiplayer game and play against computer AI bots. You can spectate/observe this game however.",
                                              config.MinLevel));
                    actionsDescription += string.Format("{0} cannot play, his level is {1}, minimum level is {2}\n",
                                                        p.account.Name,
                                                        p.account.Level,
                                                        config.MinLevel);
                    ok = false;
                }
                else if (config.MinElo != null && p.account.EffectiveElo < config.MinElo) {
                    SpecPlayerOnCondition(p.player,
                                          p.account,
                                          string.Format("Sorry, minimum elo skill is {0} on this host. You can spectate/observe this game however.",
                                                        config.MinElo));
                    actionsDescription += string.Format("{0} cannot play, his elo is {1}, minimum elo is {2}\n",
                                                        p.account.Name,
                                                        p.account.EffectiveElo,
                                                        config.MinElo);
                    ok = false;
                }
            }
            return ok;
        }

        static BalanceTeamsResult PerformBalance(BattleContext context,
                                                 bool isGameStart,
                                                 int? allyCount,
                                                 bool? clanWise,
                                                 AutohostConfig config,
                                                 int playerCount) {
            var res = new BalanceTeamsResult();
            AutohostMode mode = context.GetMode();

            using (var db = new ZkDataContext()) {
                if (!CheckPlayersMinimumConditions(context, db, config, ref res.Message)) {
                    res.CanStart = false;
                    return res;
                }

                switch (mode) {
                    case AutohostMode.None:
                    {
                        if (!isGameStart) res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise ?? false, context);
                    }
                        break;
                    case AutohostMode.SmallTeams:
                    case AutohostMode.Experienced:
                    case AutohostMode.BigTeams:
                    {
                        Resource map = db.Resources.Single(x => x.InternalName == context.Map);
                        if (map.MapFFAMaxTeams != null) res = new Balancer().LegacyBalance(allyCount ?? map.MapFFAMaxTeams.Value, clanWise ?? true, context);
                        else res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise ?? true, context);
                        res.DeleteBots = true;
                        return res;
                    }
                    case AutohostMode.Game1v1:
                    {
                        res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise ?? true, context);
                        res.DeleteBots = true;
                    }
                        break;

                    case AutohostMode.GameChickens:
                    {
                        res.Players = context.Players.ToList();
                        res.Bots = context.Bots.Where(x => x.Owner != context.AutohostName).ToList();
                        foreach (PlayerTeam p in res.Players) p.AllyID = 0;
                        foreach (BotTeam b in res.Bots) b.AllyID = 1;

                        if (!res.Bots.Any() && res.Players.Count > 0) {
                            res.Message = "Add some bot (computer player) as your enemy. Use button on bottom left. Chicken or CAI is recommended.";
                            res.CanStart = false;
                            /*else
                                    {
                                        res.Bots.Add(new BotTeam() { AllyID = 1, TeamID = 16, BotName = "default_Chicken", BotAI = "Chicken: Normal", });
                                        res.Message = "Adding a normal chickens bot for you";
                                    }*/
                        }
                    }
                        break;
                    case AutohostMode.GameFFA:
                    {
                        Resource map = db.Resources.Single(x => x.InternalName == context.Map);
                        if (map.MapFFAMaxTeams != null) res = new Balancer().LegacyBalance(allyCount ?? map.MapFFAMaxTeams.Value, clanWise ?? true, context);
                        else res = new Balancer().LegacyBalance(allyCount ?? map.MapFFAMaxTeams ?? 8, clanWise ?? true, context);
                        return res;
                    }
                    case AutohostMode.Planetwars:
                        return new Balancer().PlanetwarsBalance(context);
                }
                return res;
            }
        }

        void RecursiveBalance(int itemIndex) {
            if (iterationsChecked > 2000000) return;

            if (itemIndex < balanceItems.Count) {
                BalanceItem item = balanceItems[itemIndex];

                if (item.CanBeMoved) {
                    foreach (BalanceTeam team in teams) {
                        if (team.Count + item.Count <= maxTeamSize) {
                            team.AddItem(item);
                            RecursiveBalance(itemIndex + 1);
                            team.RemoveItem(item);
                        }
                    }
                }
                else RecursiveBalance(itemIndex + 1);
            }
            else {
                // end of recursion
                iterationsChecked++;
                double stdDev = GetTeamsDifference(teams);
                if (stdDev < bestStdDev) {
                    bestStdDev = stdDev;
                    bestTeams = CloneTeams(teams);
                }
            }
        }

        static void SplitAutohost(BattleContext context) {
            TasClient tas = Global.Nightwatch.Tas;
            try {
                //find first one that isnt running and is using same mode (by name)
                Battle splitTo =
                    tas.ExistingBattles.Values.FirstOrDefault(
                        x =>
                        !x.Founder.IsInGame && x.NonSpectatorCount == 0 && x.Founder.Name != context.AutohostName && !x.IsPassworded &&
                        x.Founder.Name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9') ==
                        context.AutohostName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'));

                if (splitTo != null) {
                    // set same map 
                    tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!map " + context.Map, false);

                    var db = new ZkDataContext();
                    List<int?> ids = context.Players.Where(y => !y.IsSpectator).Select(x => (int?)x.LobbyID).ToList();
                    List<Account> users = db.Accounts.Where(x => ids.Contains(x.LobbyID)).ToList();
                    var toMove = new List<Account>();

                    double moveCount = Math.Ceiling(users.Count/2.0);
                    if (users.Count%2 == 0 && users.Count%4 != 0) {
                        // in case of say 18 people, move 10 nubs out, keep 8 pros
                        moveCount = users.Count/2 + 1;
                    }

                    // split while keeping clan groups together
                    foreach (var clanGrp in users.GroupBy(x => x.ClanID ?? x.LobbyID).OrderBy(x => x.Average(y => y.EffectiveElo))) {
                        toMove.AddRange(clanGrp);
                        if (toMove.Count >= moveCount) break;
                    }

                    PlayerJuggler.SuppressJuggler = true;
                    foreach (Account m in toMove) tas.ForceJoinBattle(m.Name, splitTo.BattleID);
                    Thread.Sleep(5000);
                    if (context.GetMode() == AutohostMode.Planetwars)
                    {
                        tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!map", false);
                        tas.Say(TasClient.SayPlace.User, context.AutohostName, "!map", false);
                    }
                    tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!start", false);
                    tas.Say(TasClient.SayPlace.User, context.AutohostName, "!start", false);
                    Thread.Sleep(3000);
                    if (!tas.ExistingUsers[splitTo.Founder.Name].IsInGame) {
                        tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!cbalance", false);
                        tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!forcestart", false);
                    }
                    if (!tas.ExistingUsers[context.AutohostName].IsInGame) {
                        tas.Say(TasClient.SayPlace.User, context.AutohostName, "!cbalance", false);
                        tas.Say(TasClient.SayPlace.User, context.AutohostName, "!forcestart", false);
                    }
                    PlayerJuggler.SuppressJuggler = false;
                }
            } catch (Exception ex) {
                tas.Say(TasClient.SayPlace.User, "Licho[0K]", ex.ToString(), false);
            }
        }

        static void VerifySpecCheaters(BattleContext context, BalanceTeamsResult res) {
            try {
                // find specs with same IP as some player and kick them
                using (var db = new ZkDataContext()) {
                    List<int?> ids = context.Players.Select(y => (int?)y.LobbyID).ToList();
                    Dictionary<int?, string> ipByLobbyID = db.Accounts.Where(x => ids.Contains(x.LobbyID)).ToDictionary(x => x.LobbyID,
                                                                                                                        x =>
                                                                                                                        x.AccountIPS.OrderByDescending
                                                                                                                            (y => y.LastLogin).First()
                                                                                                                            .IP);
                    // lobbyid -> ip mapping

                    AutohostMode mode = context.GetMode();
                    // kick same ip specs for starred and non chickens
                    /*
                    if (mode != AutohostMode.None && mode != AutohostMode.GameChickens) {
						foreach (var p in context.Players.Where(x => x.IsSpectator)) {
							var ip = ipByLobbyID[p.LobbyID];
							if (context.Players.Any(x => !x.IsSpectator && ipByLobbyID[x.LobbyID] == ip)) Global.Nightwatch.Tas.AdminKickFromLobby(p.Name, "Spectators from same location as players are not allowed here!");
						}
					}*/

                    foreach (var grp in context.Players.GroupBy(x => ipByLobbyID[x.LobbyID]).Where(x => x.Count() > 1)) res.Message += string.Format("\nThese people are in same location: {0}", string.Join(", ", grp.Select(x => x.Name)));
                }
            } catch (Exception ex) {
                Trace.TraceError("Error checking speccheaters: {0}", ex);
            }
        }

        #region Nested type: BalanceItem

        public class BalanceItem
        {
            public readonly int Count;
            public readonly double EloSum;
            public readonly List<int> LobbyId;
            public bool CanBeMoved = true;

            public BalanceItem(params Account[] accounts) {
                LobbyId = accounts.Select(x => x.LobbyID ?? 0).ToList();
                EloSum = accounts.Sum(x => x.EffectiveElo);
                Count = accounts.Length;
            }
        }

        #endregion

        #region Nested type: BalanceTeam

        public class BalanceTeam
        {
            public List<BalanceItem> Items = new List<BalanceItem>();
            public double AvgElo { get; private set; }
            public int Count { get; private set; }
            public double EloSum { get; private set; }

            public void AddItem(BalanceItem item) {
                Items.Add(item);
                EloSum += item.EloSum;
                Count += item.Count;
                if (Count > 0) AvgElo = EloSum/Count;
                else AvgElo = 0;
            }

            public BalanceTeam Clone() {
                var clone = new BalanceTeam();
                clone.Items = new List<BalanceItem>(Items);
                clone.AvgElo = AvgElo;
                clone.EloSum = EloSum;
                clone.Count = Count;
                return clone;
            }

            public void RemoveItem(BalanceItem item) {
                Items.Remove(item);
                EloSum -= item.EloSum;
                Count -= item.Count;
                if (Count > 0) AvgElo = EloSum/Count;
                else AvgElo = 0;
            }
        }

        #endregion
    }
}