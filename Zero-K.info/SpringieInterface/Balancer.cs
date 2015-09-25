using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using LobbyClient;
using PlasmaShared;
using ZkData;
using ZkLobbyServer;

namespace ZeroKWeb.SpringieInterface
{
    public class Balancer
    {
        const double MaxCbalanceDifference = 40;
        const double MaxTeamSizeDifferenceRatio = 2;

        public enum BalanceMode
        {
            Normal,
            ClanWise,
            FactionWise
        }

        List<BalanceItem> balanceItems;

        double bestStdDev = double.MaxValue;
        List<BalanceTeam> bestTeams;
        long iterationsChecked;
        int maxTeamSize;
        readonly List<BalanceTeam> teams = new List<BalanceTeam>();

        public static BalanceTeamsResult BalanceTeams(BattleContext context, bool isGameStart, int? allyCount, bool? clanWise) {
            var config = context.GetConfig();
            var playerCount = context.Players.Count(x => !x.IsSpectator);

            // game is managed, is bigger than split limit and wants to start -> split into two and issue starts
            if (isGameStart && context.GetMode() != AutohostMode.None && config.SplitBiggerThan != null && config.SplitBiggerThan < playerCount) {
                new Thread(() =>
                    {
                        try {
                            SplitAutohost(context, true);
                        } catch (Exception ex) {
                            Trace.TraceError("Error when splitting game:{0}", ex);
                        }
                    }).Start();
                return new BalanceTeamsResult
                       {
                           CanStart = false,
                           Message =
                               string.Format("Game too big - splitting into two - max players is {0} here",
                                             config.SplitBiggerThan)
                       };
            }

            
            if (clanWise == null && (config.AutohostMode == AutohostMode.Generic || config.AutohostMode == AutohostMode.Teams)) clanWise = true;

            var res = PerformBalance(context, isGameStart, allyCount, clanWise, config, playerCount);

            if (context.GetMode() != AutohostMode.Planetwars) // planetwars skip other checks
            {

                if (isGameStart)
                {
                    if (playerCount < (config.MinToStart ?? 0))
                    {
                        res.Message = string.Format("This host needs at least {0} people to start", config.MinToStart);
                        res.CanStart = false;
                        return res;
                    }
                    if (playerCount > (config.MaxToStart ?? 99))
                    {
                        res.Message = string.Format("This host can only start with at most {0} players", config.MaxToStart);
                        res.CanStart = false;
                        return res;
                    }

                    // dont allow to start alone
                    if (playerCount <= 1)
                    {
                        if (res.DeleteBots)
                            return new BalanceTeamsResult
                            {
                                CanStart = false,
                                Message = "You cannot play alone on this host, wait for players or join another game room and play with bots."
                            };
                        if (!context.Bots.Any())
                            return new BalanceTeamsResult
                            {
                                CanStart = false,
                                Message = "Cannot play alone, you can add bots using button on bottom left."
                            };
                    }
                }
            }
            if (isGameStart) VerifySpecCheaters(context, res);

            return res;
        }


        public static List<BalanceTeam> CloneTeams(List<BalanceTeam> t) {
            return new List<BalanceTeam>(t.Select(x => x.Clone()));
        }

        public static double GetTeamsDifference(List<BalanceTeam> t) {
            if (t.Count == 2) return Math.Abs(t[0].AvgElo - t[1].AvgElo);
            var min = Double.MaxValue;
            var max = Double.MinValue;
            foreach (var team in t) {
                if (team.AvgElo > max) max = team.AvgElo;
                if (team.AvgElo < min) min = team.AvgElo;
            }
            return max - min;
        }

        /// <summary>
        /// Split a too-large game into two equivalent smaller games
        /// </summary>
        /// <param name="context"></param>
        /// <param name="forceStart">Start the game as soon as the split occurs; so no chance for the players to escape</param>
        public static void SplitAutohost(BattleContext context, bool forceStart = false) {
            var server = Global.Server;
            try
            {
                //find first one that isnt running and is using same mode (by name)
                var splitTo =
                    server.Battles.Values.FirstOrDefault(
                        x =>
                        !x.Founder.IsInGame && x.NonSpectatorCount == 0 && x.Founder.Name != context.AutohostName && !x.IsPassworded &&
                        x.Founder.Name.TrimNumbers() ==
                        context.AutohostName.TrimNumbers());

                if (splitTo != null)
                {
                    // set same map 
                    server.GhostPm(splitTo.Founder.Name, "!map " + context.Map);

                    var db = new ZkDataContext();
                    var ids = context.Players.Where(y => !y.IsSpectator).Select(x => (int?)x.LobbyID).ToList();
                    var users = db.Accounts.Where(x => ids.Contains(x.AccountID)).ToList();
                    var toMove = new List<Account>();

                    var moveCount = Math.Ceiling(users.Count / 2.0);

                    /*if (users.Count%2 == 0 && users.Count%4 != 0) {
                        // in case of say 18 people, move 10 nubs out, keep 8 pros
                        moveCount = users.Count/2 + 1;
                    }*/

                    // split while keeping clan groups together
                    // note disabled splittinhg by clan - use "x.ClanID ?? x.LobbyID" for clan balance
                    foreach (var clanGrp in users.GroupBy(x => x.ClanID ?? x.AccountID).OrderBy(x => x.Average(y => y.EffectiveElo)))
                    {
                        toMove.AddRange(clanGrp);
                        if (toMove.Count >= moveCount) break;
                    }

                    try
                    {
                        foreach (var m in toMove) server.ForceJoinBattle(m.Name, splitTo.FounderName);
                        Thread.Sleep(5000);
                        server.GhostPm(context.AutohostName, "!lock 180");
                        server.GhostPm(splitTo.Founder.Name, "!lock 180");
                        if (context.GetMode() == AutohostMode.Planetwars)
                        {
                            server.GhostPm(context.AutohostName, "!map");
                            Thread.Sleep(500);
                            server.GhostPm(splitTo.Founder.Name, "!map");
                        }
                        else server.GhostPm(splitTo.Founder.Name, "!map " + context.Map);
                        if (forceStart)
                        {
                            server.GhostPm(splitTo.Founder.Name, "!balance");
                            server.GhostPm(context.AutohostName, "!balance");
                            server.GhostPm(splitTo.Founder.Name, "!forcestart");
                            server.GhostPm(context.AutohostName, "!forcestart");
                        }

                        server.GhostPm(context.AutohostName, "!endvote");
                        server.GhostPm(splitTo.Founder.Name, "!endvote");

                        server.GhostPm(context.AutohostName, "!start");
                        server.GhostPm(splitTo.Founder.Name, "!start");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error when splitting: {0}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        /// <summary>
        /// Force spec all players who don't meet the Elo or level requirements
        /// </summary>
        /// <param name="battleContext"></param>
        /// <param name="dataContext">The Zero-K database to check <see cref="Account"/> account details against</param>
        /// <param name="config"></param>
        /// <param name="actionsDescription"></param>
        /// <returns>True if no players need to be specced, false otherwise</returns>
        static bool CheckPlayersMinimumConditions(BattleContext battleContext,
                                                  ZkDataContext dataContext,
                                                  AutohostConfig config,
                                                  ref string actionsDescription) {
            var ok = true;
            foreach (
                var p in
                    battleContext.Players.Where(x => !x.IsSpectator)
                                 .Select(x => new { player = x, account = dataContext.Accounts.First(y => y.AccountID == x.LobbyID) })) {
                if ((config.MinLevel != null && p.account.Level < config.MinLevel) || (config.MaxLevel != null && p.account.Level > config.MaxLevel) ||
                    (config.MinElo != null && p.account.EffectiveElo < config.MinElo) ||
                    (config.MaxElo != null && p.account.EffectiveElo > config.MaxElo)) {
                    SpecPlayerOnCondition(p.player,
                                          p.account,
                                          string.Format(
                                              "Sorry, you cannot play here because of skill limits. You can spectate/observe this game however."));
                    actionsDescription += string.Format("{0} cannot play here because of skill limits\n", p.account.Name);
                    ok = false;
                }
            }
            return ok;
        }

        /// <summary>
        /// The function that actually moves players arounds
        /// </summary>
        /// <param name="teamCount"></param>
        /// <param name="mode"></param>
        /// <param name="b"></param>
        /// <param name="unmovablePlayers"></param>
        /// <returns></returns>
        BalanceTeamsResult LegacyBalance(int teamCount, BalanceMode mode, BattleContext b, params List<Account>[] unmovablePlayers) {
            var ret = new BalanceTeamsResult();

            try {
                ret.CanStart = true;
                ret.Players = b.Players.ToList();

                var db = new ZkDataContext();
                var nonSpecList = b.Players.Where(y => !y.IsSpectator).Select(y => (int?)y.LobbyID).ToList();
                var accs =
                    db.Accounts.Where(x => nonSpecList.Contains(x.AccountID)).ToList();
                if (accs.Count < 1) {
                    ret.CanStart = false;
                    return ret;
                }
                if (teamCount < 1) teamCount = 1;
                if (teamCount > accs.Count) teamCount = accs.Count;
                if (teamCount == 1) {
                    foreach (var p in ret.Players) p.AllyID = 0;
                    return ret;
                }

                maxTeamSize = (int)Math.Ceiling(accs.Count/(double)teamCount);

                teams.Clear();
                for (var i = 0; i < teamCount; i++) {
                    var team = new BalanceTeam();
                    teams.Add(team);
                    if (unmovablePlayers != null && unmovablePlayers.Length > i) {
                        var unmovables = unmovablePlayers[i];
                        team.AddItem(new BalanceItem(unmovablePlayers[i].ToArray()) { CanBeMoved = false });
                        accs.RemoveAll(x => unmovables.Any(y => y.AccountID == x.AccountID));
                    }
                }

                balanceItems = new List<BalanceItem>();
                if (mode == BalanceMode.ClanWise) {
                    var clanGroups = accs.GroupBy(x => x.ClanID ?? x.AccountID).ToList();
                    if (teamCount > clanGroups.Count() || clanGroups.Any(x => x.Count() > maxTeamSize)) mode = BalanceMode.Normal;
                    else balanceItems.AddRange(clanGroups.Select(x => new BalanceItem(x.ToArray())));
                }
                if (mode == BalanceMode.FactionWise) {
                    balanceItems.Clear();
                    var factionGroups = accs.GroupBy(x => x.FactionID ?? x.AccountID).ToList();
                    balanceItems.AddRange(factionGroups.Select(x => new BalanceItem(x.ToArray())));
                } 

                if (mode == BalanceMode.Normal) {
                    balanceItems.Clear();
                    balanceItems.AddRange(accs.Select(x => new BalanceItem(x)));
                }

                var sw = new Stopwatch();
                sw.Start();
                RecursiveBalance(0);
                sw.Stop();

                if (bestTeams == null)
                {
                    var fallback = new Balancer().LegacyBalance(teamCount, BalanceMode.ClanWise, b, null);
                    fallback.Message += "\nWarning: STANDARD TEAM BALANCE USED, PlanetWars not possible with those teams, too many from one faction";
                    return fallback;
                }

                var minSize = bestTeams.Min(x => x.Count);
                var maxSize = bestTeams.Max(x => x.Count);
                var sizesWrong = maxSize/(double)minSize > MaxTeamSizeDifferenceRatio;

                // cbalance failed, rebalance using normal
                if (mode == BalanceMode.ClanWise && (bestTeams == null || GetTeamsDifference(bestTeams) > MaxCbalanceDifference || sizesWrong))
                    return new Balancer().LegacyBalance(teamCount, BalanceMode.Normal, b, unmovablePlayers);
                        // cbalance failed, rebalance using normal

                if (sizesWrong && mode == BalanceMode.FactionWise) {
                    var fallback = new Balancer().LegacyBalance(teamCount, BalanceMode.ClanWise, b, null);
                    fallback.Message += "\nWarning: STANDARD TEAM BALANCE USED, PlanetWars not possible with those teams, too many from one faction";
                    return fallback ; // fallback standard balance if PW balance fails
                    /*ret.CanStart = false;
                    ret.Message = string.Format("Failed to balance - too many people from same faction");
                    return ret;*/
                }

                if (unmovablePlayers != null && unmovablePlayers.Length > 0) {
                    var minElo = bestTeams.Min(x => x.AvgElo);
                    var maxElo = bestTeams.Max(x => x.AvgElo);
                    if (maxElo - minElo > GlobalConst.MaxPwEloDifference) {
                        var fallback = new Balancer().LegacyBalance(teamCount, BalanceMode.ClanWise, b, null);
                        fallback.Message += "\nWarning: STANDARD TEAM BALANCE USED, PlanetWars not possible with those teams, too many from one faction";
                        return fallback; // fallback standard balance if PW balance fails
                        /*
                        ret.CanStart = false;
                        ret.Message = string.Format("Team difference is too big - win chance {0}% - spectate some or wait for more people",
                                                    Utils.GetWinChancePercent(maxElo - minElo));
                        return ret;*/
                    }
                }

                if (bestTeams == null) {
                    ret.CanStart = false;
                    ret.Message =
                        string.Format(
                            "Failed to balance {0} - too many people from same clan or faction (in teams game you can try !random and !forcestart)");
                    return ret;
                }
                else {
                    if (unmovablePlayers == null || unmovablePlayers.Length == 0) bestTeams = bestTeams.Shuffle(); // permute when not unmovable players present

                    var text = "( ";

                    var lastTeamElo = 0.0;
                    var allyNum = 0;
                    foreach (var team in bestTeams) {
                        if (allyNum > 0) text += " : ";
                        text += string.Format("{0}", (allyNum + 1));
                        if (allyNum > 0) text += string.Format("={0}%)", Utils.GetWinChancePercent(lastTeamElo - team.AvgElo));
                        lastTeamElo = team.AvgElo;

                        foreach (var u in team.Items.SelectMany(x => x.LobbyId)) ret.Players.Single(x => x.LobbyID == u).AllyID = allyNum;
                        allyNum++;
                    }
                    text += ")";

                    ret.Message = String.Format("{0} players balanced {2} to {1} teams {3}. {4} combinations checked, spent {5}ms of CPU time",
                                                bestTeams.Sum(x => x.Count),
                                                teamCount,
                                                mode,
                                                text,
                                                iterationsChecked,
                                                sw.ElapsedMilliseconds);
                }
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
                ret.Message = ex.ToString();
                ret.CanStart = false;
            }
            return ret;
        }

        /// <summary>
        /// Calls <see cref="LegacyBalance"/> with the appropriate parameters depending on game settings and conditions
        /// </summary>
        /// <param name="isGameStart">If true and <see cref="AutohostMode"/> is none, do nothing (i.e. don't autobalance custom rooms at start)</param>
        /// <param name="allyCount"></param>
        /// <param name="clanWise"></param>
        /// <param name="config"></param>
        /// <param name="playerCount"></param>
        /// <remarks>Also removes bots from team games, and tells people to add bots to a chicken game if absent</remarks>
        static BalanceTeamsResult PerformBalance(BattleContext context,
                                                 bool isGameStart,
                                                 int? allyCount,
                                                 bool? clanWise,
                                                 AutohostConfig config,
                                                 int playerCount) {
            var res = new BalanceTeamsResult();
            var mode = context.GetMode();

            using (var db = new ZkDataContext()) {
                if (!CheckPlayersMinimumConditions(context, db, config, ref res.Message)) {
                    res.CanStart = false;
                    return res;
                }

                switch (mode) {
                    case AutohostMode.None:
                    {
                        if (!isGameStart) res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise == true ? BalanceMode.ClanWise : BalanceMode.Normal, context);
                    }
                        break;
                    case AutohostMode.Generic:
                    {
                        if (allyCount == null && res.Bots != null && res.Bots.Any())
                        {
                            res.Players = context.Players.ToList();
                            res.Bots = context.Bots.Where(x => x.Owner != context.AutohostName).ToList();
                            foreach (var p in res.Players) p.AllyID = 0;
                            foreach (var b in res.Bots) b.AllyID = 1;
                        }
                        else
                        {
                            var map = db.Resources.Single(x => x.InternalName == context.Map);
                            res = new Balancer().LegacyBalance(allyCount ?? map.MapFFAMaxTeams ?? 2,
                                clanWise == false ? BalanceMode.Normal : BalanceMode.ClanWise,
                                context);
                            res.DeleteBots = mode == AutohostMode.Teams;
                        }
                        return res;
                    }
                    
                    case AutohostMode.Teams:
                    {
                        var map = db.Resources.Single(x => x.InternalName == context.Map);
                        res = new Balancer().LegacyBalance(allyCount ?? map.MapFFAMaxTeams ?? 2, clanWise == false ? BalanceMode.Normal : BalanceMode.ClanWise, context);
                        res.DeleteBots = mode == AutohostMode.Teams;
                        return res;
                    }
                    case AutohostMode.Game1v1:
                    {
                        res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise == false ? BalanceMode.Normal : BalanceMode.ClanWise, context);
                        res.DeleteBots = true;
                    }
                        break;

                    case AutohostMode.GameChickens:
                    {
                        res.Players = context.Players.ToList();
                        res.Bots = context.Bots.Where(x => x.Owner != context.AutohostName).ToList();
                        foreach (var p in res.Players) p.AllyID = 0;
                        foreach (var b in res.Bots) b.AllyID = 1;

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
                        var map = db.Resources.Single(x => x.InternalName == context.Map);
                        if (map.MapFFAMaxTeams != null)
                            res = new Balancer().LegacyBalance(allyCount ?? map.MapFFAMaxTeams.Value,
                                                               clanWise == false ? BalanceMode.Normal : BalanceMode.ClanWise,
                                                               context);
                        else
                            res = new Balancer().LegacyBalance(allyCount ?? map.MapFFAMaxTeams ?? 8,
                                                               clanWise == false ? BalanceMode.Normal : BalanceMode.ClanWise,
                                                               context);
                        return res;
                    }
                    case AutohostMode.Planetwars:

                        return new Balancer().PlanetwarsBalance(context);
                }
                return res;
            }
        }

        BalanceTeamsResult PlanetwarsBalance(BattleContext context) {
            var res = new BalanceTeamsResult();
            res.CanStart = true;
            res.DeleteBots = true;

            using (var db = new ZkDataContext()) {
                res.Message = "";
                var planet = db.Galaxies.Single(x => x.IsDefault).Planets.First(x => x.Resource.InternalName == context.Map);

                var info = Global.PlanetWarsMatchMaker.GetBattleInfo(context.AutohostName);
                if (info == null)
                {
                    res.Message = "Start battle using matchmaker";
                    res.CanStart = false;
                    return res;
                } 

                foreach (string matchUser in info.Attackers)
                {
                    AddPwPlayer(context, matchUser, res, 0);
                }

                foreach (string matchUser in info.Defenders)
                {
                    AddPwPlayer(context, matchUser, res, 1);
                }

                foreach (var p in context.Players.Where(x => !res.Players.Any(y => y.Name == x.Name)))
                {
                    p.IsSpectator = true;
                    res.Players.Add(p);
                }

                var teamNum = 1;
                foreach (var p in res.Players)
                {
                    p.TeamID = teamNum++; // normalize teams
                }
                
                // bots game
                int cnt = 0;
                if (planet.PlanetStructures.Any(x => !string.IsNullOrEmpty(x.StructureType.EffectBots)))
                {
                    foreach (var b in planet.PlanetStructures.Select(x => x.StructureType).Where(x => !string.IsNullOrEmpty(x.EffectBots))) res.Bots.Add(new BotTeam { AllyID = 2, BotAI = b.EffectBots, BotName = "Aliens" + cnt++ });

                    res.Message += string.Format("This planet is infested by aliens, fight for your survival");
                    return res;
                }

                
                return res;
            }
        }

        static void AddPwPlayer(BattleContext context, string matchUser, BalanceTeamsResult res, int allyID)
        {
            PlayerTeam player = context.Players.FirstOrDefault(x => x.Name == matchUser);
            if (player == null)
            {
                player = new PlayerTeam() { Name = matchUser };
                ConnectedUser us;
                if (Global.Server.ConnectedUsers.TryGetValue(matchUser, out us)) player.LobbyID = us.User.AccountID;
                else
                {
                    var db = new ZkDataContext();
                    var acc = Account.AccountByName(db, matchUser);
                    if (acc != null) player.LobbyID = acc.AccountID;
                }
            }
            res.Players.Add(new PlayerTeam { AllyID = allyID , IsSpectator = false, Name = player.Name, LobbyID = player.LobbyID, TeamID = player.TeamID });
        }

        /// <summary>
        /// Gets the best balance (lowest standard deviation between teams)
        /// </summary>
        void RecursiveBalance(int itemIndex) {
            if (iterationsChecked > 2000000) return;

            if (itemIndex < balanceItems.Count) {
                var item = balanceItems[itemIndex];

                if (item.CanBeMoved) {
                    foreach (var team in teams) {
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
                var stdDev = GetTeamsDifference(teams);
                if (stdDev < bestStdDev) {
                    bestStdDev = stdDev;
                    bestTeams = CloneTeams(teams);
                }
            }
        }

        static void SpecPlayerOnCondition(PlayerTeam player, Account account, string userMessage) {
            player.IsSpectator = true;
            Global.Server.GhostPm(account.Name, userMessage);
        }

        /// <summary>
        /// Makes <see cref="Springie"/> print a message if two or more people have the same IP
        /// </summary>
        /// <param name="context"></param>
        /// <param name="res">The <see cref="BalanceTeamsResult"/> to write the message to</param>
        static void VerifySpecCheaters(BattleContext context, BalanceTeamsResult res) {
            try {
                // find specs with same IP as some player and kick them
                using (var db = new ZkDataContext()) {
                    var ids = context.Players.Select(y => (int?)y.LobbyID).ToList();
                    var ipByLobbyID = db.Accounts.Where(x => ids.Contains(x.AccountID))
                                        .ToDictionary(x => x.AccountID, x => x.AccountIPs.OrderByDescending(y => y.LastLogin).Select(y=>y.IP).FirstOrDefault());
                    // lobbyid -> ip mapping

                    var mode = context.GetMode();
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

        public class BalanceItem
        {
            public bool CanBeMoved = true;
            public readonly int Count;
            public readonly double EloSum;
            public readonly List<int> LobbyId;

            public BalanceItem(params Account[] accounts) {
                LobbyId = accounts.Select(x => x.AccountID).ToList();
                EloSum = accounts.Sum(x => x.EffectiveElo);
                Count = accounts.Length;
            }
        }

        public class BalanceTeam
        {
            public double AvgElo { get; private set; }
            public int Count { get; private set; }
            public double EloSum { get; private set; }
            public List<BalanceItem> Items = new List<BalanceItem>();

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
    }
}