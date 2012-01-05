using System;
using System.Collections.Generic;
using System.Linq;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.AppCode
{
    public class BalanceTeamsResult
    {
        public List<AccountTeam> BalancedTeams = new List<AccountTeam>();
        public List<BotTeam> Bots = new List<BotTeam>();
        public bool DeleteBots;
        public string Message;
    }

    public class AccountTeam
    {
        public int AccountID;
        public int AllyID;
        public string Name;
        public bool Spectate;
        public int TeamID;
    }

    public class BotTeam
    {
        public int AllyID;
        public string BotAI;
        public string BotName;
        public string Owner;
        public int TeamID;
    }

    public class Balancer
    {

        public static BalanceTeamsResult BalanceTeams(string autoHost, string map, string mod,List<AccountTeam> currentTeams, List<BotTeam> currentBots)
        {
            var mode = ContentService.GetModeFromHost(autoHost);
            if (currentTeams.Count < 1) return new BalanceTeamsResult();
            using (var db = new ZkDataContext())
            {
                var res = new BalanceTeamsResult();
                res.Message = "";
                var idList = currentTeams.Select(x => x.AccountID).ToList();
                var players = new List<Account>();

                foreach (var p in idList.Select(x => db.Accounts.First(y => y.LobbyID == x)))
                {
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
                    if (p.Level < GlobalConst.MinPlanetWarsLevel)
                    {
                        res.Message += string.Format("{0} cannot play, his level is {1}, minimum level is {2}\n",
                                                     p.Name,
                                                     p.Level,
                                                     GlobalConst.MinPlanetWarsLevel);
                        AuthServiceClient.SendLobbyMessage(p,
                                                           string.Format(
                                                               "Sorry, PlanetWars is competive online campaign for experienced players. You need to be at least level 5 to play here. To increase your level, play more games on other hosts or open multiplayer game and play against computer AI bots. You can observe this game however."));
                    }
                    else players.Add(p);
                }
                var clans = players.Where(x => x.Clan != null).Select(x => x.Clan).ToList();
                var treaties = new Dictionary<Tuple<Clan, Clan>, EffectiveTreaty>();
                var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == map);

                // bots game
                if (planet.PlanetStructures.Any(x => !string.IsNullOrEmpty(x.StructureType.EffectBots)))
                {
                    var teamID = 0;
                    for (var i = 0; i < players.Count; i++)
                        res.BalancedTeams.Add(new AccountTeam()
                                              { AccountID = players[i].LobbyID ?? 0, Name = players[i].Name, AllyID = 0, TeamID = teamID++ });
                    int cnt = 1;
                    foreach (var b in planet.PlanetStructures.Select(x => x.StructureType).Where(x => !string.IsNullOrEmpty(x.EffectBots))) res.Bots.Add(new BotTeam() { AllyID = 1, BotAI = b.EffectBots, TeamID = teamID++, BotName = "Aliens" + cnt++});

                    res.Message += string.Format("This planet is infested by aliens, fight for your survival");
                    return res;
                }

                var planetFactionId = planet.Account != null ? planet.Account.FactionID ?? 0 : 0;
                var attackerFactions =
                    planet.AccountPlanets.Where(x => x.DropshipCount > 0 && x.Account.FactionID != null).Select(x => (x.Account.FactionID ?? 0)).
                        Distinct().ToList();

                if (currentTeams.Count < 2) return new BalanceTeamsResult() { Message = "Not enough players" };

                for (var i = 1; i < clans.Count; i++)
                {
                    for (var j = 0; j < i; j++)
                    {
                        var treaty = clans[i].GetEffectiveTreaty(clans[j]);
                        treaties[Tuple.Create(clans[i], clans[j])] = treaty;
                        treaties[Tuple.Create(clans[j], clans[i])] = treaty;

                        // if treaty is neutral but they send ships - mark as "war"
                        if (planet.OwnerAccountID != null && treaty.AllyStatus == AllyStatus.Neutral)
                        {
                            if (clans[i].ClanID == planet.Account.ClanID &&
                                planet.AccountPlanets.Any(x => x.Account.ClanID == clans[j].ClanID && x.DropshipCount > 0)) treaty.AllyStatus = AllyStatus.War;
                            else if (clans[j].ClanID == planet.Account.ClanID &&
                                     planet.AccountPlanets.Any(x => x.Account.ClanID == clans[i].ClanID && x.DropshipCount > 0)) treaty.AllyStatus = AllyStatus.War;
                        }
                    }
                }

                var sameTeamScore = new double[players.Count,players.Count];
                for (var i = 1; i < players.Count; i++)
                {
                    for (var j = 0; j < i; j++)
                    {
                        var c1 = players[i].Clan;
                        var c2 = players[j].Clan;
                        var f1 = players[i].FactionID ?? -1;
                        var f2 = players[i].FactionID ?? -1;
                        var points = 0.0;
                        if (players[i].FactionID != null && players[i].FactionID == players[j].FactionID) points = 3; // same faction weight 1
                        if (c1 != null && c2 != null)
                        {
                            if (c1 == c2) points = 4;
                            else
                            {
                                var treaty = treaties[Tuple.Create(players[i].Clan, players[j].Clan)];
                                if (treaty.AllyStatus == AllyStatus.Alliance) points = 2;
                                else if (treaty.AllyStatus == AllyStatus.Ceasefire) points = 1;
                                else if (treaty.AllyStatus == AllyStatus.War) points = -3;
                                if (treaty.AllyStatus == AllyStatus.Neutral && f1 != f2)
                                    if ((planetFactionId == f1 && attackerFactions.Contains(f2)) ||
                                        (planetFactionId == f2 && attackerFactions.Contains(f1))) points = -3;
                            }
                        }
                        else if (f1 != f2)
                            if ((planetFactionId == f1 && attackerFactions.Contains(f2)) ||
                                (planetFactionId == f2 && attackerFactions.Contains(f1))) points = -3;

                        sameTeamScore[i, j] = points;
                        sameTeamScore[j, i] = points;
                        //res.Message += string.Format("{0} + {1} = {2} \n", players[i].Name, players[j].Name, points);
                    }
                }

                var playerScoreMultiplier = new double[players.Count];
                for (var i = 0; i < players.Count; i++)
                {
                    var mult = 1.0;
                    var player = players[i];
                    if (planet.OwnerAccountID == player.AccountID) mult += 1; // owner 
                    else if (planet.Account != null && planet.Account.ClanID == player.AccountID) mult += 0.5; // owner's clan 
                    if (planet.AccountPlanets.Any(x => x.AccountID == player.AccountID && x.DropshipCount > 0)) mult += 1; // own dropship 
                    else if (planet.AccountPlanets.Any(x => x.DropshipCount > 0 && x.Account.ClanID == player.ClanID)) mult += 0.5; // clan's dropship 
                    playerScoreMultiplier[i] = mult;

                    //res.Message += string.Format("{0} mult = {1} \n", players[i].Name, mult);
                }

                var limit = 1 << (players.Count);
                var bestCombination = -1;
                var bestScore = double.MinValue;
                double bestCompo = 0;
                double absCompo = 0;
                double bestElo = 0;
                double bestTeamDiffs = 0;
                var playerAssignments = new int[players.Count];
                for (var combinator = 0; combinator < limit; combinator++)
                {
                    //double team0Weight = 0;
                    double team0Elo = 0;
                    //double team1Weight = 0;
                    double team1Elo = 0;
                    var team0count = 0;
                    var team1count = 0;

                    // determine where each player is amd dp some adding
                    for (var i = 0; i < players.Count; i++)
                    {
                        var player = players[i];
                        var team = (combinator & (1 << i)) > 0 ? 1 : 0;
                        playerAssignments[i] = team;
                        if (team == 0)
                        {
                            team0Elo += player.EffectiveElo;
                            //team0Weight += player.EloWeight;
                            team0count++;
                        }
                        else
                        {
                            team1Elo += player.EffectiveElo; // *player.EloWeight;
                            //team1Weight += player.EloWeight;
                            team1count++;
                        }
                    }
                    if (team0count == 0 || team1count == 0) continue; // skip combination, empty team

                    // calculate score for team difference
                    var teamDiffScore = -(20.0*Math.Abs(team0count - team1count)/(double)(team0count + team1count)) -
                                        Math.Abs(team0count - team1count);
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
                                            select
                                                teamData.GroupBy(x => players[x].FactionID).Where(x => x.Key != null && x.Count() >= majorityCount).
                                                Select(x => x.Key ?? 0)).ToList();
                    if (majorityFactions.Count == 2 && majorityFactions[0].Intersect(majorityFactions[1]).Any()) continue; // winning either side would be benefitial for some majority faction

                    // calculate score for meaningfull teams
                    var compoScore = 0.0;
                    for (var i = 0; i < players.Count; i++) // for every player calculate his score as average of relations to other plaeyrs
                    {
                        double sum = 0;
                        var cnt = 0;
                        for (var j = 0; j < players.Count; j++)
                        {
                            if (i != j)
                            {
                                var sts = sameTeamScore[i, j];
                                if (sts != 0.0) // we only consider no-neutral people 
                                {
                                    if (playerAssignments[i] == playerAssignments[j])
                                    {
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

                    if (score > bestScore)
                    {
                        bestCombination = combinator;
                        bestScore = score;
                        bestElo = eloScore;
                        bestCompo = compoScore;
                        bestTeamDiffs = teamDiffScore;
                    }
                }

                if (bestCombination == -1)
                {
                    res.BalancedTeams = null;
                    res.Message += "Cannot be balanced well at this point";
                }
                    /*else if (bestCompo < absCompo*0.5)
                {
                    res.BalancedTeams = null;
                    res.Message += string.Format("Cannot be balanced well at this point - best composition: {0}, available: {1}", absCompo, bestCompo);
                }*/
                else
                {
                    var differs = false;
                    for (var i = 0; i < players.Count; i++)
                    {
                        var allyID = ((bestCombination & (1 << i)) > 0) ? 1 : 0;
                        if (!differs && allyID != currentTeams.First(x => x.AccountID == players[i].LobbyID).AllyID) differs = true;
                        res.BalancedTeams.Add(new AccountTeam()
                                              { AccountID = players[i].LobbyID.Value, Name = players[i].Name, AllyID = allyID, TeamID = i });
                    }
                    if (differs)
                    {
                        res.Message +=
                            string.Format(
                                "Winning combination  score: {0:0.##} team difference,  {1:0.##} elo,  {2:0.##} composition. Win chance {3}%",
                                bestTeamDiffs,
                                bestElo,
                                bestCompo,
                                Utils.GetWinChancePercent(bestElo*20));
                    }
                }
                res.DeleteBots = true;
                return res;
            }
        }
    }
}