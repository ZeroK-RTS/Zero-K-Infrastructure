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
        private List<BalanceItem> balanceItems;

        private double bestStdDev = double.MaxValue;
        private int maxTeamSize;
        private List<BalanceTeam> teams = new List<BalanceTeam>();

        public static BalanceTeamsResult BalanceTeams(BattleContext context, bool isGameStart, int? allyCount, bool? clanWise) {
            var config = context.GetConfig();
            var playerCount = context.Players.Count(x => !x.IsSpectator);

            // game is managed, is bigger than split limit and wants to start -> split into two and issue starts
            if (isGameStart && context.GetMode() != AutohostMode.None && config.SplitBiggerThan != null && config.SplitBiggerThan < playerCount) {
                new Thread(() =>
                    {
                        try {
                            SplitAutohost(context);
                        } catch {}
                        ;
                    }).Start();
                return new BalanceTeamsResult() { CanStart = false, Message = string.Format("Game too big - splitting into two - max players is {0} here", config.SplitBiggerThan) };
            }

            if (clanWise == null && (config.AutohostMode == AutohostMode.MediumTeams || config.AutohostMode == AutohostMode.BigTeams || config.AutohostMode == AutohostMode.SmallTeams)) clanWise = true;

            var res = PerformBalance(context, isGameStart, allyCount, clanWise, config, playerCount);

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
                if (config.AutohostMode == AutohostMode.Game1v1 || config.AutohostMode == AutohostMode.GameFFA || config.AutohostMode == AutohostMode.None) res.CanStart = false;
            }

            if (isGameStart) VerifySpecCheaters(context, res);

            return res;
        }


        private BalanceTeamsResult LegacyBalance(int teamCount, bool clanwise, BattleContext b) {
            var ret = new BalanceTeamsResult();

            try {
                ret.CanStart = true;
                ret.Players = b.Players.ToList();

                var db = new ZkDataContext();
                var accs = db.Accounts.Where(x => b.Players.Where(y => !y.IsSpectator).Select(y => (int?)y.LobbyID).ToList().Contains(x.LobbyID)).ToList();
                if (accs.Count < 1) {
                    ret.CanStart = false;
                    return ret;
                }
                if (teamCount < 1) teamCount = 1;
                if (teamCount > accs.Count) teamCount = accs.Count;
                if (teamCount == 1)
                {
                    foreach (var p in ret.Players) p.AllyID = 0;
                    return ret;
                }

                maxTeamSize = (int)Math.Ceiling(accs.Count / (double)teamCount);

                balanceItems = new List<BalanceItem>();
                if (clanwise) {
                    var clanGroups = accs.GroupBy(x => x.ClanID ?? x.LobbyID).ToList();
                    if (teamCount > clanGroups.Count() || clanGroups.Any(x => x.Count() > maxTeamSize))
                    {
                        clanwise = false;
                    }
                    else {
                        balanceItems.AddRange(clanGroups.Select(x=>new BalanceItem(x.ToArray())));
                    }
                }
                if (!clanwise) {
                    balanceItems.Clear();
                    balanceItems.AddRange(accs.Select(x=>new BalanceItem(x)));
                }

                for (var i = 0; i < teamCount; i++) teams.Add(new BalanceTeam());
                teamAssignments = new List<int>();
                for (var i = 0; i < balanceItems.Count; i++) teamAssignments[i] = -1;

                var sw = new Stopwatch();
                sw.Start();
                RecursiveBalance(0);
                sw.Stop();

                if (bestTeamAssignments == null) {
                    ret.CanStart = false;
                    ret.Message = string.Format("Failed to balance {0}", (clanwise ? "- too many people from same clan? Use !balance and !forcestart" : ". Use !random and !forcestart"));
                }
                else {
                    for (int i =0; i< teamAssignments.Count;i++) {
                        teams[teamAssignments[i]].Items.Add(balanceItems[i]);
                    }
                    teams = teams.Shuffle(); // permute

                    var text = "(ratings ";

                    var lastTeamElo = 0.0;
                    var allyNum = 0;
                    foreach (var team in teams) {
                        if (allyNum > 0) text += " : ";
                        text += string.Format("{0}={1}", (allyNum + 1), Math.Round(team.AvgElo));
                        if (allyNum > 0) text += string.Format(" ({0}%)", Utils.GetWinChancePercent(lastTeamElo - team.AvgElo));
                        lastTeamElo = team.AvgElo;

                        foreach (var u in team.Items.SelectMany(x => x.LobbyId)) {
                            ret.Players.Single(x => x.LobbyID == u).AllyID = allyNum;
                        }
                        allyNum++;
                    }
                    text += ")";

                    ret.Message = String.Format("{0} players balanced {2} to {1} teams {3}. {4} combinations checked, spent {5}ms of CPU time", accs.Count, teamCount, clanwise ? "respecting clans" : "", text, iterationsChecked, sw.ElapsedMilliseconds);
                }
            } catch (Exception ex) {
                ret.Message = ex.ToString();
                ret.CanStart = false;
            }

            return ret;
        }


        private static BalanceTeamsResult PerformBalance(BattleContext context, bool isGameStart, int? allyCount, bool? clanWise, AutohostConfig config, int playerCount) {
            var res = new BalanceTeamsResult();
            var mode = context.GetMode();

            if (mode != AutohostMode.Planetwars) {
                switch (mode) {
                    case AutohostMode.None:
                        if (!isGameStart) res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise ?? false, context);
                        break;
                    case AutohostMode.MediumTeams:
                    case AutohostMode.SmallTeams:
                    case AutohostMode.BigTeams:
                        res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise ?? false, context);
                        res.DeleteBots = true;
                        break;
                    case AutohostMode.Game1v1:
                        res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise ?? false, context);
                        res.DeleteBots = true;
                        break;

                    case AutohostMode.GameChickens:
                        res.Players = context.Players.ToList();
                        res.Bots = context.Bots.Where(x => x.Owner != context.AutohostName).ToList();
                        foreach (var p in res.Players) {
                            p.AllyID = 0;
                        }
                        foreach (var b in res.Bots) {
                            b.AllyID = 1;
                        }
                        if (!res.Bots.Any()) {
                            if (res.Players.Count > 0) {
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
                        var db = new ZkDataContext();
                        var map = db.Resources.Single(x => x.InternalName == context.Map);
                        if (isGameStart) {
                            if (map.MapFFAMaxTeams != null) res = new Balancer().LegacyBalance(map.MapFFAMaxTeams.Value, false, context);
                        }
                        else res = new Balancer().LegacyBalance(allyCount ?? map.MapFFAMaxTeams ?? 8, false, context);
                        break;
                }
                return res;
            }
            else {
                context.Players = context.Players.Where(x => !x.IsSpectator).ToList();
                res.CanStart = false;
                res.DeleteBots = true;
                if (playerCount <= 1) return res;

                using (var db = new ZkDataContext()) {
                    res.Message = "";
                    var idList = context.Players.Select(x => x.LobbyID).ToList();
                    var players = new List<Account>();

                    foreach (var p in idList.Select(x => db.Accounts.First(y => y.LobbyID == x))) {
                        /*if (p.ClanID == null)
                        {
                            //res.Message += string.Format("{0} cannot play, must join a clan first http://zero-k.info/Planetwars/ClanList\n", p.Name);
                            //AuthServiceClient.SendLobbyMessage(p, "To play here, join a clan first http://zero-k.info/Planetwars/ClanList");
                        }*/
                        /*if (p.Clan != null && !p.Name.Contains(p.Clan.Shortcut))
                        {
                            res.Message += string.Format("{0} cannot play, name must contain clan tag {1}\n", p.Name, p.Clan.Shortcut);
                            AuthServiceClient.SendLobbyMessage(p,
                                                               string.Format(
                                                                   "Your name must contain clan tag {0}, rename for example by saying: /rename [{0}]{1}",
                                                                   p.Clan.Shortcut,
                                                                   p.Name));
                        }*/
                        if (p.Level < config.MinLevel) {
                            res.Message += string.Format("{0} cannot play, his level is {1}, minimum level is {2}\n", p.Name, p.Level, config.MinLevel);
                            AuthServiceClient.SendLobbyMessage(p,
                                                               string.Format(
                                                                   "Sorry, PlanetWars is competive online campaign for experienced players. You need to be at least level 5 to play here. To increase your level, play more games on other hosts or open multiplayer game and play against computer AI bots. You can observe this game however."));
                        }
                        else players.Add(p);
                    }
                    var clans = players.Where(x => x.Clan != null).Select(x => x.Clan).ToList();
                    var treaties = new Dictionary<Tuple<Clan, Clan>, EffectiveTreaty>();
                    var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == context.Map);

                    // bots game
                    if (planet.PlanetStructures.Any(x => !string.IsNullOrEmpty(x.StructureType.EffectBots))) {
                        var teamID = 0;
                        for (var i = 0; i < players.Count; i++) {
                            res.Players.Add(new PlayerTeam() { LobbyID = players[i].LobbyID ?? 0, Name = players[i].Name, AllyID = 0, TeamID = teamID++ });
                        }
                        var cnt = 1;
                        foreach (var b in planet.PlanetStructures.Select(x => x.StructureType).Where(x => !string.IsNullOrEmpty(x.EffectBots))) {
                            res.Bots.Add(new BotTeam() { AllyID = 1, BotAI = b.EffectBots, TeamID = teamID++, BotName = "Aliens" + cnt++ });
                        }

                        res.Message += string.Format("This planet is infested by aliens, fight for your survival");
                        return res;
                    }

                    var planetFactionId = planet.Account != null ? planet.Account.FactionID ?? 0 : 0;
                    var attackerFactions = planet.AccountPlanets.Where(x => x.DropshipCount > 0 && x.Account.FactionID != null).Select(x => (x.Account.FactionID ?? 0)).Distinct().ToList();

                    if (context.Players.Count < 2) return new BalanceTeamsResult() { Message = "Not enough players", CanStart = false };

                    for (var i = 1; i < clans.Count; i++) {
                        for (var j = 0; j < i; j++) {
                            var treaty = clans[i].GetEffectiveTreaty(clans[j]);
                            treaties[Tuple.Create(clans[i], clans[j])] = treaty;
                            treaties[Tuple.Create(clans[j], clans[i])] = treaty;

                            // if treaty is neutral but they send ships - mark as "war"
                            if (planet.OwnerAccountID != null && treaty.AllyStatus == AllyStatus.Neutral) {
                                if (clans[i].ClanID == planet.Account.ClanID && planet.AccountPlanets.Any(x => x.Account.ClanID == clans[j].ClanID && x.DropshipCount > 0)) treaty.AllyStatus = AllyStatus.War;
                                else if (clans[j].ClanID == planet.Account.ClanID && planet.AccountPlanets.Any(x => x.Account.ClanID == clans[i].ClanID && x.DropshipCount > 0)) treaty.AllyStatus = AllyStatus.War;
                            }
                        }
                    }

                    var sameTeamScore = new double[players.Count,players.Count];
                    for (var i = 1; i < players.Count; i++) {
                        for (var j = 0; j < i; j++) {
                            var c1 = players[i].Clan;
                            var c2 = players[j].Clan;
                            var f1 = players[i].FactionID ?? -1;
                            var f2 = players[i].FactionID ?? -1;
                            var points = 0.0;
                            if (players[i].FactionID != null && players[i].FactionID == players[j].FactionID) points = 3; // same faction weight 1
                            if (c1 != null && c2 != null) {
                                if (c1 == c2) points = 4;
                                else {
                                    var treaty = treaties[Tuple.Create(players[i].Clan, players[j].Clan)];
                                    if (treaty.AllyStatus == AllyStatus.Alliance) points = 2;
                                    else if (treaty.AllyStatus == AllyStatus.Ceasefire) points = 1;
                                    else if (treaty.AllyStatus == AllyStatus.War) points = -3;
                                    if (treaty.AllyStatus == AllyStatus.Neutral && f1 != f2) if ((planetFactionId == f1 && attackerFactions.Contains(f2)) || (planetFactionId == f2 && attackerFactions.Contains(f1))) points = -3;
                                }
                            }
                            else if (f1 != f2) if ((planetFactionId == f1 && attackerFactions.Contains(f2)) || (planetFactionId == f2 && attackerFactions.Contains(f1))) points = -3;

                            sameTeamScore[i, j] = points;
                            sameTeamScore[j, i] = points;
                            //res.Message += string.Format("{0} + {1} = {2} \n", players[i].Name, players[j].Name, points);
                        }
                    }

                    var playerScoreMultiplier = new double[players.Count];
                    for (var i = 0; i < players.Count; i++) {
                        var mult = 1.0;
                        var player = players[i];
                        if (planet.OwnerAccountID == player.AccountID) mult += 1; // owner 
                        else if (planet.Account != null && planet.Account.ClanID == player.AccountID) mult += 0.5; // owner's clan 
                        if (planet.AccountPlanets.Any(x => x.AccountID == player.AccountID && x.DropshipCount > 0)) mult += 1; // own dropship 
                        else if (planet.AccountPlanets.Any(x => x.DropshipCount > 0 && x.Account.ClanID == player.ClanID)) mult += 0.5; // clan's dropship 
                        playerScoreMultiplier[i] = mult;

                        //res.Message += string.Format("{0} mult = {1} \n", players[i].Name, mult);
                    }

                    var limit = (long)1 << (players.Count);
                    long bestCombination = -1;
                    var bestScore = double.MinValue;
                    double bestCompo = 0;
                    double absCompo = 0;
                    double bestElo = 0;
                    double bestTeamDiffs = 0;
                    var playerAssignments = new int[players.Count];
                    for (var combinator = (long)0; combinator < limit; combinator++) {
                        //double team0Weight = 0;
                        double team0Elo = 0;
                        //double team1Weight = 0;
                        double team1Elo = 0;
                        var team0count = 0;
                        var team1count = 0;

                        // determine where each player is amd dp some adding
                        for (var i = 0; i < players.Count; i++) {
                            var player = players[i];
                            var team = (combinator & ((long)1 << i)) > 0 ? 1 : 0;
                            playerAssignments[i] = team;
                            if (team == 0) {
                                team0Elo += player.EffectiveElo;
                                //team0Weight += player.EloWeight;
                                team0count++;
                            }
                            else {
                                team1Elo += player.EffectiveElo; // *player.EloWeight;
                                //team1Weight += player.EloWeight;
                                team1count++;
                            }
                        }
                        if (team0count == 0 || team1count == 0) continue; // skip combination, empty team

                        // calculate score for team difference
                        var teamDiffScore = -(20.0*Math.Abs(team0count - team1count)/(double)(team0count + team1count)) - Math.Abs(team0count - team1count);
                        if (teamDiffScore < -10) continue; // max imabalance

                        double balanceModifier = 0;
                        // count elo vs balance modifier
                        /*
                        if (team0count < team1count) balanceModifier = -teamDiffScore;
                        else balanceModifier = teamDiffScore;*/

                        // calculate score for elo difference

                        team0Elo = team0Elo/team0count;
                        team1Elo = team1Elo/team1count;
                        //team0Elo = team0Elo/team0Weight;
                        //team1Elo = team1Elo/team1Weight;
                        var eloScore = -Math.Abs(team0Elo - team1Elo)/14;
                        if (eloScore < -17) continue;

                        if (team0Elo < team1Elo) balanceModifier += -eloScore;
                        else balanceModifier += eloScore;

                        // verify if ther eis sense in playing (no zero sum game ip abuse)
                        var majorityFactions = (from teamData in Enumerable.Range(0, players.Count).GroupBy(x => playerAssignments[x])
                                                let majorityCount = Math.Ceiling(teamData.Count()/2.0)
                                                select teamData.GroupBy(x => players[x].FactionID).Where(x => x.Key != null && x.Count() >= majorityCount).Select(x => x.Key ?? 0)).ToList();
                        if (majorityFactions.Count == 2 && majorityFactions[0].Intersect(majorityFactions[1]).Any()) continue; // winning either side would be benefitial for some majority faction

                        // calculate score for meaningfull teams
                        var compoScore = 0.0;
                        for (var i = 0; i < players.Count; i++) // for every player calculate his score as average of relations to other plaeyrs
                        {
                            double sum = 0;
                            var cnt = 0;
                            for (var j = 0; j < players.Count; j++) {
                                if (i != j) {
                                    var sts = sameTeamScore[i, j];
                                    if (sts != 0.0) // we only consider no-neutral people 
                                    {
                                        if (playerAssignments[i] == playerAssignments[j]) {
                                            sum += sts;
                                            cnt++;
                                        }
                                        /*else sum -= sts; // different teams - score is equal to negation of same team score
                                        cnt++;*/
                                    }
                                }
                            }
                            if (cnt > 0) // player can be meaningfully ranked, he had at least one non zero relation
                                compoScore += playerScoreMultiplier[i]*sum/cnt;
                        }

                        if (compoScore < 0) continue; // get meaningfull teams only   || compoScore < 0.5*absCompo
                        if (compoScore > absCompo) absCompo = compoScore; // todo lame - abs compo not known at this point,should be 2 pass
                        var score = -Math.Abs(balanceModifier) + teamDiffScore + compoScore;

                        if (score > bestScore) {
                            bestCombination = combinator;
                            bestScore = score;
                            bestElo = eloScore;
                            bestCompo = compoScore;
                            bestTeamDiffs = teamDiffScore;
                        }
                    }

                    if (bestCombination == -1) {
                        res.Players = null;
                        res.CanStart = false;
                        res.Message += "Cannot be balanced well at this point";
                    }
                        /*else if (bestCompo < absCompo*0.5)
            {
                res.BalancedTeams = null;
                res.Message += string.Format("Cannot be balanced well at this point - best composition: {0}, available: {1}", absCompo, bestCompo);
            }*/
                    else {
                        var differs = false;
                        for (var i = 0; i < players.Count; i++) {
                            var allyID = ((bestCombination & ((long)1 << i)) > 0) ? 1 : 0;
                            if (!differs && allyID != context.Players.First(x => x.LobbyID == players[i].LobbyID).AllyID) differs = true;
                            res.Players.Add(new PlayerTeam() { LobbyID = players[i].LobbyID.Value, Name = players[i].Name, AllyID = allyID, TeamID = i });
                        }
                        if (differs) {
                            res.Message += string.Format("Winning combination  score: {0:0.##} team difference,  {1:0.##} elo,  {2:0.##} composition. Win chance {3}%",
                                                         bestTeamDiffs,
                                                         bestElo,
                                                         bestCompo,
                                                         Utils.GetWinChancePercent(bestElo*20));
                        }
                        res.CanStart = true;
                    }
                    res.DeleteBots = true;
                    return res;
                }
            }
        }

        int iterationsChecked;

        private void RecursiveBalance(int itemIndex) {
            iterationsChecked++;
            if (itemIndex < balanceItems.Count) 
            {
                var item = balanceItems[itemIndex];

                for (var teamid = 0; teamid < teams.Count; teamid++) {
                    var team = teams[teamid];
                    if (team.Count + item.Count <= maxTeamSize) {
                        teamAssignments[itemIndex] = teamid;
                        team.AddItemElo(item);
                        RecursiveBalance(itemIndex + 1);
                        team.RemoveItemElo(item);
                    }
                }
            }
            else
            {// end of recursion
                var stdDev = teams.Select(x => x.AvgElo).StdDevSquared();
                if (stdDev < bestStdDev)
                {
                    bestStdDev = stdDev;
                    bestTeamAssignments = new List<int>(teamAssignments);
                }
            }
        }

        private static void SplitAutohost(BattleContext context) {
            var tas = Global.Nightwatch.Tas;
            try {
                //find first one that isnt running and is using same mode (by name)
                var splitTo =
                    tas.ExistingBattles.Values.FirstOrDefault(
                        x =>
                        !x.Founder.IsInGame && x.NonSpectatorCount == 0 && x.Founder.Name != context.AutohostName && !x.IsPassworded &&
                        x.Founder.Name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9') == context.AutohostName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'));

                if (splitTo != null) {
                    // set same map 
                    tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!map " + context.Map, false);

                    var db = new ZkDataContext();
                    var ids = context.Players.Where(y => !y.IsSpectator).Select(x => (int?)x.LobbyID).ToList();
                    var users = db.Accounts.Where(x => ids.Contains(x.LobbyID)).ToList();
                    var toMove = new List<Account>();

                    // split while keeping clan groups together
                    foreach (var clanGrp in users.GroupBy(x => x.ClanID ?? x.LobbyID).OrderByDescending(x => x.Average(y => y.EffectiveElo))) {
                        toMove.AddRange(clanGrp);
                        if (toMove.Count >= users.Count/2) break;
                    }

                    PlayerJuggler.SuppressJuggler = true;
                    foreach (var m in toMove) {
                        tas.ForceJoinBattle(m.Name, splitTo.BattleID);
                    }
                    Thread.Sleep(5000);
                    tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!start", false);
                    tas.Say(TasClient.SayPlace.User, context.AutohostName, "!start", false);
                    Thread.Sleep(2000);
                    tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!start", false);
                    tas.Say(TasClient.SayPlace.User, context.AutohostName, "!start", false);
                    Thread.Sleep(2000);
                    tas.Say(TasClient.SayPlace.User, splitTo.Founder.Name, "!start", false);
                    tas.Say(TasClient.SayPlace.User, context.AutohostName, "!start", false);
                    PlayerJuggler.SuppressJuggler = false;
                }
            } catch (Exception ex) {
                tas.Say(TasClient.SayPlace.User, "Licho[0K]", ex.ToString(), false);
            }
        }

        private static void VerifySpecCheaters(BattleContext context, BalanceTeamsResult res) {
            try {
                // find specs with same IP as some player and kick them
                using (var db = new ZkDataContext()) {
                    var ids = context.Players.Select(y => (int?)y.LobbyID).ToList();
                    var ipByLobbyID = db.Accounts.Where(x => ids.Contains(x.LobbyID)).ToDictionary(x => x.LobbyID, x => x.AccountIPS.OrderByDescending(y => y.LastLogin).First().IP);
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

                    foreach (var grp in context.Players.GroupBy(x => ipByLobbyID[x.LobbyID]).Where(x => x.Count() > 1)) {
                        res.Message += string.Format("\nThese people are in same location: {0}", string.Join(", ", grp.Select(x => x.Name)));
                    }
                }
            } catch (Exception ex) {
                Trace.TraceError("Error checking speccheaters: {0}", ex);
            }
        }

        public class BalanceItem
        {
            public readonly int Count;
            public readonly double EloSum;
            public readonly List<int> LobbyId;

            public BalanceItem(params Account[] accounts) {
                LobbyId = accounts.Select(x => x.LobbyID ?? 0).ToList();
                EloSum = accounts.Sum(x => x.EffectiveElo);
                Count = accounts.Length;
            }
        }

        List<int> teamAssignments;
        List<int> bestTeamAssignments;


        public class BalanceTeam
        {
            public double AvgElo {
                get { return EloSum / Count; }
            }
            public int Count { get; private set; }
            public double EloSum { get; private set; }
            public List<BalanceItem> Items = new List<BalanceItem>();


            public void AddItemElo(BalanceItem item) {
                EloSum += item.EloSum;
                Count += item.Count;
            }

            public void RemoveItemElo(BalanceItem item) {
                EloSum -= item.EloSum;
                Count -= item.Count;
            }
        }
    }
}