using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web.Services;
using PlasmaShared;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb
{
    /// <summary>
    /// Summary description for ContentService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
        // [System.Web.Script.Services.ScriptService]
    public class ContentService: WebService
    {
        [WebMethod]
        public string AutohostPlayerJoined(string autohostName, string mapName, int accountID)
        {
            var db = new ZkDataContext();
            var mode = GetModeFromHost(autohostName);
            if (mode == AutohostMode.Planetwars)
            {
                var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == mapName);
                var account = db.Accounts.FirstOrDefault(x => x.LobbyID == accountID);

                // conscription
                /*
                if (account.FactionID == null)
                {
                    var rand = new Random();
                    var faclist = db.Factions.ToList();
                    var fac = faclist[rand.Next(faclist.Count)];
                    account.FactionID = fac.FactionID;
                    db.Events.InsertOnSubmit(Global.CreateEvent("{0} was conscripted by {1}", account, fac));
                    db.SubmitChanges();
                    AuthServiceClient.SendLobbyMessage(account,
                                                       string.Format(
                                                           "You must be in a faction to play PlanetWars.  You were conscripted by {0}. To change your faction go to http://zero-k.info/PlanetWars/ClanList ",
                                                           fac.Name));
                    return string.Format("Sending {0} to {1}", account.Name, fac.Name);
                }
                 */
                 
                if (account.Level < GlobalConst.MinPlanetWarsLevel)
                {
                    AuthServiceClient.SendLobbyMessage(account, "Sorry, PlanetWars is competive online campaign for experienced players. You need to be at least level 5 to play here. To increase your level, play more games on other hosts or open multiplayer game and play against computer AI bots.  You can observe this game however.");

                }

                if (account.Clan == null)
                {
                    //AuthServiceClient.SendLobbyMessage(account, "To play here, join a clan first http://zero-k.info/Planetwars/ClanList");
                    return
                        string.Format(
                            "{0} this is competetive PlanetWars campaign server. Join a clan to conquer the galaxy http://zero-k.info/Planetwars/ClanList",
                            account.Name);
                }

                
                /*if (!account.Name.Contains(account.Clan.Shortcut))
                {
                    AuthServiceClient.SendLobbyMessage(account,
                                                       string.Format(
                                                           "Your name must contain clan tag {0}, rename for example by saying: \"/rename [{0}]{1}\" or \"/rename {0}_{1}\".",
                                                           account.Clan.Shortcut,
                                                           account.Name));
                    return string.Format("{0} cannot play, name must contain clan tag {1}", account.Name, account.Clan.Shortcut);
                }*/
                var owner = "";
                if (planet.Account != null) owner = planet.Account.Name;
                return string.Format("Greetings {0} {1} of {2}, welcome to {3} planet {4} http://zero-k.info/PlanetWars/Planet/{5}",
                                     account.IsClanFounder ? account.Clan.LeaderTitle : "",
                                     account.Name,
                                     account.IsClanFounder ? account.Clan.ClanName : account.Clan.Shortcut,
                                     owner,
                                     planet.Name,
                                     planet.PlanetID);
            }
            return null;
        }


        [WebMethod]
        public BalanceTeamsResult BalanceTeams(string autoHost, string map, List<AccountTeam> currentTeams)
        {
            var mode = GetModeFromHost(autoHost);
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
                    if (p.Level < GlobalConst.MinPlanetWarsLevel) {
                        res.Message += string.Format("{0} cannot play, his level is {1}, minimum level is {2}\n", p.Name, p.Level, GlobalConst.MinPlanetWarsLevel);
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
                    foreach (var b in planet.PlanetStructures.Select(x => x.StructureType).Where(x => !string.IsNullOrEmpty(x.EffectBots))) res.Bots.Add(new BotTeam() { AllyID = 1, BotName = b.EffectBots, TeamID = teamID++ });

                    res.Message += string.Format("This planet is infested by aliens, fight for your survival");
                    return res;
                }


                int planetFactionId = planet.Account != null ? planet.Account.FactionID??0 : 0;
                var attackerFactions = planet.AccountPlanets.Where(x => x.DropshipCount > 0 && x.Account.FactionID!=null).Select(x => (x.Account.FactionID??0)).Distinct().ToList();

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
                        var f1 = players[i].FactionID??-1;
                        var f2 = players[i].FactionID??-1;
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
                                {
                                    if ((planetFactionId == f1 && attackerFactions.Contains(f2)) || (planetFactionId == f2 && attackerFactions.Contains(f1))) points = -3;
                                }
                            }
                        }
                        else {
                            if (f1 != f2) {
                                if ((planetFactionId == f1 && attackerFactions.Contains(f2)) || (planetFactionId == f2 && attackerFactions.Contains(f1))) points = -3;
                            }
                        }

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
                    if (team0count < team1count) balanceModifier = -teamDiffScore;
                    else balanceModifier = teamDiffScore;

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
                                    if (playerAssignments[i] == playerAssignments[j]) sum += sts;
                                    else sum -= sts; // different teams - score is equal to negation of same team score
                                    cnt++;
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

                return res;
            }
        }

        [WebMethod]
        public bool DownloadFile(string internalName,
                                 out List<string> links,
                                 out byte[] torrent,
                                 out List<string> dependencies,
                                 out ResourceType resourceType,
                                 out string torrentFileName)
        {
            return PlasmaServer.DownloadFile(internalName, out links, out torrent, out dependencies, out resourceType, out torrentFileName);
        }

        [WebMethod]
        public EloInfo GetEloByAccountID(int accountID)
        {
            var db = new ZkDataContext();
            var user = db.Accounts.FirstOrDefault(x => x.LobbyID == accountID);
            var ret = new EloInfo();
            if (user != null)
            {
                ret.Elo = user.EffectiveElo;
                ret.Weight = user.EloWeight;
            }
            return ret;
        }

        [WebMethod]
        public EloInfo GetEloByName(string name)
        {
            var db = new ZkDataContext();
            var user = db.Accounts.FirstOrDefault(x => x.Name == name && x.LobbyID != null && !x.IsDeleted);
            var ret = new EloInfo();
            if (user != null)
            {
                ret.Elo = user.EffectiveElo;
                ret.Weight = user.EloWeight;
            }
            return ret;
        }

        [WebMethod]
        public List<string> GetEloTop10()
        {
            var db = new ZkDataContext();
            return
                db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > DateTime.UtcNow.AddMonths(-1))).OrderByDescending(
                    x => x.Elo).Select(x => x.Name).Take(10).ToList();
        }

        /// <summary>
        /// This is backup function, remove when not needed
        /// </summary>
        public static AutohostMode GetModeFromHost(string hostname)
        {
            // hack this whole function is hack
            if (hostname.StartsWith("PlanetWars")) return AutohostMode.Planetwars;
            else return AutohostMode.GameTeams;
        }


        public class PlanetPickEntry {
            readonly Planet planet;
            readonly int weight;
            public Planet Planet { get { return planet; } }
            public int Weight { get { return weight; } }

            public PlanetPickEntry(Planet planet, int weight)
            {
                this.planet = planet;
                this.weight = weight;
            }
        }

        [WebMethod]
        public RecommendedMapResult GetRecommendedMap(string autohostName, List<AccountTeam> accounts)
        {
            var mode = GetModeFromHost(autohostName);
            var res = new RecommendedMapResult();
            using (var db = new ZkDataContext())
            {
                if (mode == AutohostMode.Planetwars)
                {
                    var playerAccounts = accounts.Where(x => !x.Spectate).Select(x => db.Accounts.First(z => z.LobbyID == x.AccountID)).ToList();
                    var playerAccountIDs = playerAccounts.Select(x => x.AccountID).ToList();
                    
                    var facGroups = playerAccounts.Where(x=>x.ClanID!=null).GroupBy(x=>x.FactionID).Select(x=>new{FactionID = x.Key, Count = x.Count()}).ToList();
                    var playerFactionIDs = facGroups.Select(x => x.FactionID).ToList();
                    List<int?> biggestFactionIDs = new List<int?>();
                    if (facGroups.Any())
                    {
                        var biggestGroup = facGroups.OrderByDescending(x => x.Count).Select(x => x.Count).FirstOrDefault();
                        biggestFactionIDs = facGroups.Where(x=>x.Count == biggestGroup).Select(x => x.FactionID).ToList();
                    }
                    

                    var gal = db.Galaxies.Single(x => x.IsDefault);

                    var valids =
                        gal.Planets.Select(
                            x =>
                            new
                            {
                                Planet = x,
                                Ships = (x.AccountPlanets.Where(y => playerAccountIDs.Contains(y.AccountID)).Sum(y => (int?)y.DropshipCount) ?? 0),
                                Defenses = (x.PlanetStructures.Where(y => !y.IsDestroyed).Sum(y => y.StructureType.EffectDropshipDefense) ?? 0)
                            }).
                            Where(x => (x.Planet.Account == null ||  playerFactionIDs.Contains(x.Planet.Account.FactionID)) && x.Ships >= x.Defenses).ToList();
                    var maxc = valids.Max(x => (int?)x.Ships) ?? 0;

                    List<PlanetPickEntry> targets = null;
                    // if there are no dropships target unclaimed and biggest clan planets - INSURGENTS
                    if (maxc == 0)
                    {
                        targets =
                            gal.Planets.Where(x => x.Account!=null && biggestFactionIDs.Contains(x.Account.FactionID)).Select(
                                x =>
                                new PlanetPickEntry(x, Math.Max(1, (2000 - x.AccountPlanets.Sum(y=>(int?)y.Influence + y.ShadowInfluence)??0)/200) - (x.PlanetStructures.Where(y=>!y.IsDestroyed).Sum(y=>y.StructureType.EffectDropshipDefense)??0))).ToList();

                        targets.AddRange(gal.Planets.Where(x => x.OwnerAccountID == null && db.Links.Any(y => (y.PlanetID1 == x.PlanetID && y.PlanetByPlanetID2.Account != null && biggestFactionIDs.Contains(y.PlanetByPlanetID2.Account.FactionID) || (y.PlanetID2 == x.PlanetID && y.PlanetByPlanetID1.Account != null && biggestFactionIDs.Contains(y.PlanetByPlanetID1.Account.FactionID))))).Select(x=>new PlanetPickEntry(x, 16 + (x.AccountPlanets.Sum(y=>(int?)y.Influence)??0) /50)));

                        if (!targets.Any()) targets = gal.Planets.Select(x => new PlanetPickEntry(x, 1)).ToList();
                    }
                    else targets = valids.Where(x => x.Ships == maxc).Select(x => new PlanetPickEntry(x.Planet, 1)).ToList();
                    // target valid planets with most dropships

                    var r = new Random(autohostName.GetHashCode() + gal.Turn); // randomizer based on autohost name + turn to always return same

                    Planet planet = null;
                    var sumw = targets.Sum(x => x.Weight);
                    if (sumw > 0)
                    {
                        var random = r.Next(sumw);
                        sumw = 0;
                        foreach (var target in targets)
                        {
                            sumw += target.Weight;
                            if (sumw >= random)
                            {
                                planet = target.Planet;
                                break;
                            }
                        }
                    }
                    if (planet == null) planet = targets[r.Next(targets.Count)].Planet; // this should not be needed;

                    res.MapName = planet.Resource.InternalName;
                    var owner = "";
                    if (planet.Account != null) owner = planet.Account.Name;

                    var shipInfo = string.Join(",",
                                               planet.AccountPlanets.Where(x => x.DropshipCount > 0 && playerAccountIDs.Contains(x.AccountID)).Select(
                                                   x => string.Format("{0} ships from {1}", x.DropshipCount, x.Account.Name)));

                    res.Message = string.Format("Welcome to {0} planet {1} http://zero-k.info/PlanetWars/Planet/{2} attacked by {3}",
                                                owner,
                                                planet.Name,
                                                planet.PlanetID,
                                                string.IsNullOrEmpty(shipInfo) ? "insurgents" : shipInfo);

                    if (planet.OwnerAccountID != null && planet.Account.Clan != null) {
                        var be = Global.Nightwatch.Tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == autohostName);
                        if (be != null && !be.Founder.IsInGame && be.MapName != res.MapName && be.NonSpectatorCount>0)
                        {
                            foreach (var a in planet.Account.Clan.Accounts)
                            {
                                AuthServiceClient.SendLobbyMessage(a,
                                                                   string.Format(
                                                                       "Your clan's planet {0} is about to be attacked! Come defend it to PlanetWars spring://@join_player:{1} ",
                                                                       planet.Name,
                                                                       autohostName));
                            }
                        }
                    }


                    db.SubmitChanges();
                }
                else
                {
                    var list =
                        db.Resources.Where(x => x.FeaturedOrder != null && x.MapIsFfa != true && x.ResourceContentFiles.Any(y => y.LinkCount > 0)).
                            ToList();
                    var r = new Random();
                    res.MapName = list[r.Next(list.Count)].InternalName;
                }
            }
            return res;
        }


        /// <summary>
        /// Finds resource by either md5 or internal name
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="internalName"></param>
        /// <returns></returns>
        [WebMethod]
        public PlasmaServer.ResourceData GetResourceData(string md5, string internalName)
        {
            return PlasmaServer.GetResourceData(md5, internalName);
        }


        [WebMethod]
        public List<PlasmaServer.ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
        {
            return PlasmaServer.GetResourceList(lastChange, out currentTime);
        }


        [WebMethod]
        public ScriptMissionData GetScriptMissionData(string name)
        {
            using (var db = new ZkDataContext())
            {
                var m = db.Missions.Single(x => x.Name == name && x.IsScriptMission);
                return new ScriptMissionData()
                       {
                           MapName = m.Map,
                           ModTag = m.ModRapidTag,
                           StartScript = m.Script,
                           ManualDependencies = m.ManualDependencies != null ? new List<string>(m.ManualDependencies.Split('\n')) : null,
                           Name = m.Name
                       };
            }
        }

        [WebMethod]
        public SpringBattleStartSetup GetSpringBattleStartSetup(string hostName,
                                                                string map,
                                                                string mod,
                                                                List<BattleStartSetupPlayer> players,
                                                                AutohostMode mode = AutohostMode.GameTeams)
        {
            try
            {
                mode = GetModeFromHost(hostName);
                var ret = new SpringBattleStartSetup();
                var commanderTypes = new LuaTable();
                var db = new ZkDataContext();

                var accountIDsWithExtraComms = new List<int>();
                // calculate to whom to send extra comms
                if (mode == AutohostMode.Planetwars)
                {
                    var groupedByTeam = players.Where(x => !x.IsSpectator).GroupBy(x => x.AllyTeam).OrderByDescending(x => x.Count());
                    var biggest = groupedByTeam.FirstOrDefault();
                    if (biggest != null)
                    {
                        foreach (var other in groupedByTeam.Skip(1))
                        {
                            var cnt = biggest.Count() - other.Count();
                            if (cnt > 0)
                            {
                                foreach (var a in
                                    other.Select(x => db.Accounts.First(y => y.LobbyID == x.AccountID)).OrderByDescending(x => x.Elo * x.EloWeight).Take
                                        (cnt)) accountIDsWithExtraComms.Add(a.AccountID);
                            }
                        }
                    }
                }

                foreach (var p in players.Where(x => !x.IsSpectator))
                {
                    var user = db.Accounts.FirstOrDefault(x => x.LobbyID == p.AccountID);
                    if (user != null)
                    {
                        var userParams = new List<SpringBattleStartSetup.ScriptKeyValuePair>();
                        ret.UserParameters.Add(new SpringBattleStartSetup.UserCustomParameters { AccountID = p.AccountID, Parameters = userParams });

                        var pu = new LuaTable();
                        var userUnlocksBanned = user.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanUnlocks);
                        var userCommandersBanned = user.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanCommanders);

                        if (!userUnlocksBanned)
                        {
                            if (mode != AutohostMode.Planetwars || user.ClanID == null) foreach (var unlock in user.AccountUnlocks.Select(x => x.Unlock)) pu.Add(unlock.Code);
                            else
                            {
                                foreach (var unlock in
                                    user.AccountUnlocks.Select(x => x.Unlock).Union(Galaxy.ClanUnlocks(db, user.ClanID).Select(x => x.Unlock))) pu.Add(unlock.Code);
                            }
                        }

                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "unlocks", Value = pu.ToBase64String() });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "faction", Value = user.Faction != null ? user.Faction.Shortcut:""});
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "clan", Value = user.Clan != null ? user.Clan.Shortcut : "" });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "level", Value = user.Level.ToString()});
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "elo", Value = user.EffectiveElo.ToString() });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "avatar", Value = user.Avatar });

                        if (accountIDsWithExtraComms.Contains(user.AccountID)) userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "extracomm", Value = "1" });

                        var pc = new LuaTable();

                        if (!userCommandersBanned)
                        {
                            foreach (var c in user.Commanders)
                            {
                                var morphTable = new LuaTable();
                                pc["[\"" + c.Name + "\"]"] = morphTable;
                                for (var i = 1; i <= 4; i++)
                                {
                                    var key = "c" + user.AccountID + "_" + c.CommanderID + "_" + i;
                                    morphTable.Add(key);

                                    var comdef = new LuaTable();
                                    commanderTypes[key] = comdef;

                                    comdef["chassis"] = c.Unlock.Code + i;

                                    var modules = new LuaTable();
                                    comdef["modules"] = modules;

                                    comdef["cost"] = c.GetTotalMorphLevelCost(i);

                                    comdef["name"] = c.Name.Substring(0, Math.Min(25, c.Name.Length)) + " level " + i;

                                    foreach (var m in
                                        c.CommanderModules.Where(x => x.CommanderSlot.MorphLevel <= i).OrderBy(x => x.Unlock.UnlockType).ThenBy(
                                            x => x.SlotID).Select(x => x.Unlock)) modules.Add(m.Code);
                                }
                            }
                        }
                        else userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "jokecomm", Value = "1" });

                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "commanders", Value = pc.ToBase64String() });
                    }
                }

                ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "commanderTypes", Value = commanderTypes.ToBase64String() });
                if (mode == AutohostMode.Planetwars)
                {
                    var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == map);

                    var owner = "";
                    var second = "";
                    var factionInfluences = planet.GetFactionInfluences().Where(x => x.Influence > 0).ToList();
                    var firstEntry = factionInfluences.FirstOrDefault();
                    var ownerAccount = planet.Account;
                    var secondEntry = factionInfluences.Skip(1).FirstOrDefault();
                    if (ownerAccount != null) owner = string.Format("{0} of {1}", ownerAccount.Clan.Shortcut, ownerAccount.Faction.Name);
                    if (secondEntry != null && firstEntry != null)
                        second = string.Format("{0} needs {1} influence - ",
                                               secondEntry.Faction.Shortcut,
                                               firstEntry.Influence - secondEntry.Influence);

                    var pwStructures = new LuaTable();
                    foreach (var s in planet.PlanetStructures.Where(x => !x.IsDestroyed && !string.IsNullOrEmpty(x.StructureType.IngameUnitName)))
                    {
                        pwStructures.Add("s" + s.StructureTypeID,
                                         new LuaTable()
                                         {
                                             { "unitname", s.StructureType.IngameUnitName },
                                             //{ "isDestroyed", s.IsDestroyed ? true : false },
                                             { "name", owner + s.StructureType.Name },
                                             { "description", second + s.StructureType.Description }
                                         });
                    }
                    ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "planetwarsStructures", Value = pwStructures.ToBase64String() });
                }

                return ret;
            }
            catch (Exception ex) {
                var db = new ZkDataContext();
                var licho = db.Accounts.SingleOrDefault(x=>x.AccountID ==5986);
                if (licho != null) foreach (var line in ex.ToString().Lines())
                        AuthServiceClient.SendLobbyMessage(licho, line);
                throw;
            }
        }


        [WebMethod]
        public void NotifyMissionRun(string login, string missionName)
        {
            using (var db = new ZkDataContext())
            using (var scope = new TransactionScope())
            {
                db.Missions.Single(x => x.Name == missionName).MissionRunCount++;
                db.Accounts.First(x => x.Name == login && x.LobbyID != null).MissionRunCount++;
                db.SubmitChanges();
                scope.Complete();
            }
        }


        [WebMethod]
        public PlasmaServer.ReturnValue RegisterResource(int apiVersion,
                                                         string springVersion,
                                                         string md5,
                                                         int length,
                                                         ResourceType resourceType,
                                                         string archiveName,
                                                         string internalName,
                                                         int springHash,
                                                         byte[] serializedData,
                                                         List<string> dependencies,
                                                         byte[] minimap,
                                                         byte[] metalMap,
                                                         byte[] heightMap,
                                                         byte[] torrentData)
        {
            return PlasmaServer.RegisterResource(apiVersion,
                                                 springVersion,
                                                 md5,
                                                 length,
                                                 resourceType,
                                                 archiveName,
                                                 internalName,
                                                 springHash,
                                                 serializedData,
                                                 dependencies,
                                                 minimap,
                                                 metalMap,
                                                 heightMap,
                                                 torrentData);
        }

        [WebMethod]
        public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds)
        {
            using (var db = new ZkDataContext())
            {
                var acc = AuthServiceClient.VerifyAccountHashed(login, passwordHash);
                if (acc == null) throw new ApplicationException("Invalid login or password");

                acc.XP += GlobalConst.XpForMissionOrBots;

                var mission = db.Missions.Single(x => x.Name == missionName);

                var scoreEntry = mission.MissionScores.FirstOrDefault(x => x.AccountID == acc.AccountID);
                if (scoreEntry == null)
                {
                    scoreEntry = new MissionScore() { MissionID = mission.MissionID, AccountID = acc.AccountID, Score = int.MinValue };
                    mission.MissionScores.Add(scoreEntry);
                }

                if (score > scoreEntry.Score)
                {
                    var max = mission.MissionScores.Max(x => (int?)x.Score);
                    if (max == null || max <= score)
                    {
                        mission.TopScoreLine = login;
                        acc.XP += 150; // 150 for getting top score
                    }
                    scoreEntry.Score = score;
                    scoreEntry.Time = DateTime.UtcNow;
                    scoreEntry.MissionRevision = mission.Revision;
                    scoreEntry.GameSeconds = gameSeconds;
                    db.SubmitChanges();
                }
            }
        }

        [WebMethod]
        public string SubmitSpringBattleResult(string accountName,
                                               string password,
                                               BattleResult result,
                                               List<BattlePlayerResult> players,
                                               List<string> extraData)
        {
            try
            {
                var acc = AuthServiceClient.VerifyAccountPlain(accountName, password);
                if (acc == null) throw new Exception("Account name or password not valid");
                if (extraData == null) extraData = new List<string>();

                var mode = GetModeFromHost(accountName);
                var db = new ZkDataContext();
                if (mode == AutohostMode.Planetwars) db.ExecuteCommand("update account set creditsincome =0, creditsexpense=0 where creditsincome<>0 or creditsexpense<>0");
                

                var sb = new SpringBattle()
                         {
                             HostAccountID = acc.AccountID,
                             Duration = result.Duration,
                             EngineGameID = result.EngineBattleID,
                             MapResourceID = db.Resources.Single(x => x.InternalName == result.Map).ResourceID,
                             ModResourceID = db.Resources.Single(x => x.InternalName == result.Mod).ResourceID,
                             HasBots = result.IsBots,
                             IsMission = result.IsMission,
                             PlayerCount = players.Count(x => !x.IsSpectator),
                             StartTime = result.StartTime,
                             Title = result.Title,
                             ReplayFileName = result.ReplayName,
                             EngineVersion = result.EngineVersion ?? "0.82.7",
                             // hack remove when fixed
                         };
                db.SpringBattles.InsertOnSubmit(sb);

                foreach (var p in players)
                {
                    sb.SpringBattlePlayers.Add(new SpringBattlePlayer()
                                               {
                                                   AccountID = db.Accounts.First(x => x.LobbyID == p.AccountID).AccountID,
                                                   AllyNumber = p.AllyNumber,
                                                   CommanderType = p.CommanderType,
                                                   IsInVictoryTeam = p.IsVictoryTeam,
                                                   IsSpectator = p.IsSpectator,
                                                   Rank = p.Rank,
                                                   LoseTime = p.LoseTime
                                               });
                }

                db.SubmitChanges();

                // awards
                foreach (var line in extraData.Where(x => x.StartsWith("award")))
                {
                    var partsSpace = line.Substring(6).Split(new[] { ' ' }, 3);
                    var name = partsSpace[0];
                    var awardType = partsSpace[1];
                    var awardText = partsSpace[2];

                    var player = sb.SpringBattlePlayers.First(x => x.Account.Name == name && x.Account.LobbyID != null);
                    db.AccountBattleAwards.InsertOnSubmit(new AccountBattleAward()
                                                          {
                                                              AccountID = player.AccountID,
                                                              SpringBattleID = sb.SpringBattleID,
                                                              AwardKey = awardType,
                                                              AwardDescription = awardText
                                                          });
                }
                db.SubmitChanges();

                var orgLevels = sb.SpringBattlePlayers.Select(x => x.Account).ToDictionary(x => x.AccountID, x => x.Level);

                sb.CalculateElo();
                try
                {
                    db.SubmitChanges();
                }
                catch (ChangeConflictException e)
                {
                    db.ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);
                    db.SubmitChanges();
                }

                var text = new StringBuilder();

                if (mode == AutohostMode.Planetwars && sb.SpringBattlePlayers.Any())
                {
                    

                    var gal = db.Galaxies.Single(x => x.IsDefault);
                    var planet = gal.Planets.Single(x => x.MapResourceID == sb.MapResourceID);

                    text.AppendFormat("Battle on http://zero-k.info/PlanetWars/Planet/{0} has ended\n", planet.PlanetID);

                    // handle infelunce
                    Faction ownerFaction = null;
                    Clan ownerClan = null;
                    List<Clan> involvedClans = new List<Clan>();
                    if (planet.Account != null)
                    {
                        ownerClan = planet.Account.Clan;
                        ownerFaction = planet.Account.Faction;
                        if (ownerClan != null) involvedClans.Add(ownerClan); // planet ownerinvolved
                    }
                    
                    // ship owners -> involved
                    var activePlayerIds = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.Account.FactionID != null).Select(x => x.AccountID).ToList();
                    bool wasShipAttacked = false;
                    foreach (var c in planet.AccountPlanets.Where(x => x.DropshipCount > 0 && activePlayerIds.Contains(x.AccountID) && x.Account!=null && x.Account.Clan!=null).GroupBy(x=>x.Account.Clan).Select(x=>x.Key)) {
                       involvedClans.Add(c);
                       wasShipAttacked = true;
                    }
                    if (!wasShipAttacked) involvedClans.Clear(); // insurgency no involved



                    var clanTechIp =
                        sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).Where(x => x.ClanID != null).GroupBy(x => x.ClanID).
                            ToDictionary(x => x.Key, z => Galaxy.ClanUnlocks(db, z.Key).Count()*6.0/z.Count());

                    var planetDefs = (planet.PlanetStructures.Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectDropshipDefense) ?? 0);
                    var totalShips = (planet.AccountPlanets.Sum(x => (int?)x.DropshipCount) ?? 0);
                    double shipMultiplier = 1;
                    if (totalShips > 0 && totalShips >= planetDefs) shipMultiplier = (totalShips - planetDefs)/(double)totalShips;

                    var ownerMalus = 0;
                    if (ownerFaction != null)
                    {
                        var entries = planet.GetFactionInfluences();
                        if (entries.Count() > 1)
                        {
                            var diff = entries.First().Influence - entries.Skip(1).First().Influence;
                            ownerMalus = Math.Min((int)((diff/100.0)*(diff/100.0)), 70);
                        }
                    }

                    // malus for ships
                    foreach (var p in sb.SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam && x.Account.FactionID != null))
                    {
                        var ships = planet.AccountPlanets.Where(x => x.AccountID == p.AccountID).Sum(x => (int?)x.DropshipCount) ?? 0;
                        if (ships <= 0) continue;
                        p.Influence = ships*GlobalConst.PlanetwarsInvadingShipLostMalus;
                        var entry = planet.AccountPlanets.SingleOrDefault(x => x.AccountID == p.AccountID);
                        if (entry == null)
                        {
                            entry = new AccountPlanet() { AccountID = p.AccountID, PlanetID = planet.PlanetID };
                            db.AccountPlanets.InsertOnSubmit(entry);
                        }
                        entry.Influence += p.Influence ?? 0;

                        db.Events.InsertOnSubmit(Global.CreateEvent("{0} lost {1} influence at {2} because of {3} ships {4}",
                                                                    p.Account,
                                                                    p.Influence ?? 0,
                                                                    planet,
                                                                    ships,
                                                                    sb));

                        text.AppendFormat("{0} lost {1} influence at {2} because of {3} ships\n", p.Account.Name, p.Influence ?? 0, planet.Name, ships);
                    }

                    foreach (var p in sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam && x.Account.FactionID != null))
                    {
                        var techBonus = p.Account.ClanID != null ? (int)clanTechIp[p.Account.ClanID] : 0;
                        var gapMalus = 0;
                        var shipBonus = 0;
                        if (ownerFaction != null && p.Account.Faction == ownerFaction) gapMalus = ownerMalus;
                        p.Influence += (techBonus - gapMalus);

                        var ships = planet.AccountPlanets.Where(x => x.AccountID == p.AccountID).Sum(x => (int?)x.DropshipCount) ?? 0;
                        shipBonus = (int)Math.Round(ships*GlobalConst.PlanetwarsInvadingShipBonus*shipMultiplier);
                        p.Influence += shipBonus;

                        if (p.Influence < 0) p.Influence = 0;

                        var entry = planet.AccountPlanets.SingleOrDefault(x => x.AccountID == p.AccountID);
                        if (entry == null)
                        {
                            entry = new AccountPlanet() { AccountID = p.AccountID, PlanetID = planet.PlanetID };
                            db.AccountPlanets.InsertOnSubmit(entry);
                        }

                        var infl = p.Influence ?? 0;

                        // is involved - is same faction as involved clan, or same clan as involved clan or allied to involved clan
                        bool isInvolved = !involvedClans.Any() || involvedClans.Any(x=>x.FactionID == p.Account.FactionID || x.ClanID == p.Account.ClanID) || (p.Account.Clan!= null && involvedClans.Any(x=> x.GetEffectiveTreaty(p.Account.Clan).AllyStatus == AllyStatus.Alliance));


                        // store influence
                        var soldStr = "";
                        if (isInvolved) entry.Influence += infl;
                        else
                        {
                            p.Account.Credits += infl*GlobalConst.NotInvolvedIpSell;
                            soldStr = string.Format("sold for ${0} to locals because wasn't directly involved", infl * GlobalConst.NotInvolvedIpSell);
                        }

                        db.Events.InsertOnSubmit(Global.CreateEvent("{0} got {1} ({4} {5} {6}) influence at {2} from {3} {7}",
                                                                    p.Account,
                                                                    p.Influence ?? 0,
                                                                    planet,
                                                                    sb,
                                                                    techBonus > 0 ? "+" + techBonus + " from techs" : "",
                                                                    gapMalus > 0 ? "-" + gapMalus + " from domination" : "",
                                                                    shipBonus > 0 ? "+" + shipBonus + " from ships" : "", soldStr));

                        text.AppendFormat("{0} got {1} ({3} {4} {5}) influence at {2} {6}\n",
                                          p.Account.Name,
                                          p.Influence ?? 0,
                                          planet.Name,
                                          techBonus > 0 ? "+" + techBonus + " from techs" : "",
                                          gapMalus > 0 ? "-" + gapMalus + " from domination" : "",
                                          shipBonus > 0 ? "+" + shipBonus + " from ships" : "", soldStr);
                    }

                    db.SubmitChanges();

                    // destroy existing dropships and prevent growth
                    var noGrowAccount = new List<int>();
                    foreach (var ap in planet.AccountPlanets.Where(x => x.DropshipCount > 0))
                    {
                        if (!sb.SpringBattlePlayers.Any(x => x.AccountID == ap.AccountID && !x.IsSpectator))
                        {
                            ap.Account.DropshipCount += ap.DropshipCount;

                            // only destroy ships if player actually played
                        }
                        ap.DropshipCount = 0;
                        noGrowAccount.Add(ap.AccountID);
                    }

                    // destroy pw structures
                    var handled = new List<string>();
                    foreach (var line in extraData.Where(x => x.StartsWith("structurekilled")))
                    {
                        var data = line.Substring(16).Split(',');
                        var unitName = data[0];
                        if (handled.Contains(unitName)) continue;
                        handled.Add(unitName);
                        foreach (var s in
                            db.PlanetStructures.Where(
                                x => x.PlanetID == planet.PlanetID && x.StructureType.IngameUnitName == unitName && !x.IsDestroyed))
                        {
                            if (s.StructureType.IsIngameDestructible)
                            {
                                if (s.StructureType.IngameDestructionNewStructureTypeID != null)
                                {
                                    db.PlanetStructures.DeleteOnSubmit(s);
                                    db.PlanetStructures.InsertOnSubmit(new PlanetStructure()
                                                                       {
                                                                           PlanetID = planet.PlanetID,
                                                                           StructureTypeID = s.StructureType.IngameDestructionNewStructureTypeID.Value,
                                                                           IsDestroyed = true
                                                                       });
                                }
                                else s.IsDestroyed = true;
                                db.Events.InsertOnSubmit(Global.CreateEvent("{0} has been destroyed on {1} planet {2}. {3}",
                                                                            s.StructureType.Name,
                                                                            ownerClan,
                                                                            planet,
                                                                            sb));
                            }
                        }
                    }
                    db.SubmitChanges();

                    // destroy structures (usually defenses)
                    foreach (var s in planet.PlanetStructures.Where(x => !x.IsDestroyed && x.StructureType.BattleDeletesThis).ToList()) planet.PlanetStructures.Remove(s);
                    db.SubmitChanges();

                    // spawn new dropships
                    foreach (var a in
                        sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).Where(
                            x => x.ClanID != null && !noGrowAccount.Contains(x.AccountID)))
                    {
                        var capacity = a.GetDropshipCapacity();
                        var income = GlobalConst.DefaultDropshipProduction +
                                     (a.Planets.SelectMany(x => x.PlanetStructures).Where(x => !x.IsDestroyed).Sum(
                                         x => x.StructureType.EffectDropshipProduction) ?? 0);

                        a.DropshipCount += income;
                        if (Math.Floor(a.DropshipCount) > capacity) // dont exceed capacity by more than 1
                        {
                            a.DropshipCount -= income;
                            if (a.DropshipCount < capacity) a.DropshipCount = capacity;
                            AuthServiceClient.SendLobbyMessage(a, "You cannot produce any more dropships, fleet capacity is full, use your ships to attack enemy planet in PlanetWars");
                        }
                        
                    }
                    db.SubmitChanges();

                    Galaxy.RemoveOrphanedShips(db);
                    db.SubmitChanges();
                    


                    // income + decay
                    foreach (var entry in gal.Planets.Where(x => x.OwnerAccountID != null))
                    {
                        var corruption = entry.GetCorruption();
                        var mineIncome = (int)((entry.GetMineIncome()*(1.0 - corruption)));

                        entry.Account.Credits -= entry.GetUpkeepCost();
                        entry.Account.Credits += mineIncome/2; // owner gets 50%

                        // remaining 50% split by same faction by influences
                        var entry1 = entry;
                        var myFactionInfluences =
                            entry.AccountPlanets.Where(x => x.Account.FactionID == entry1.Account.FactionID).Select(
                                x => new { Acc = x.Account, Influence = ((int?)x.Influence + x.ShadowInfluence) ?? 0 }).ToList();
                        var myFactionSumInflunece = myFactionInfluences.Sum(x => (int?)x.Influence) ?? 1;
                        if (myFactionSumInflunece == 0) myFactionSumInflunece = 1;
                        foreach (var myFacAcc in myFactionInfluences) myFacAcc.Acc.Credits += (int)Math.Ceiling(mineIncome/2.0*myFacAcc.Influence/(double)myFactionSumInflunece);

                        if (corruption > 0)
                        {
                            foreach (var facEntries in entry.AccountPlanets.GroupBy(x => x.Account.Faction).Where(x => x.Key != null))
                            {
                                var cnt = facEntries.Where(x => x.Influence > 0).Count();
                                if (cnt > 0)
                                {
                                    var personDecay = (int)Math.Ceiling(GlobalConst.InfluenceDecay/(double)cnt);
                                    foreach (var e in facEntries.Where(x => x.Influence > 0)) e.Influence = e.Influence - personDecay;
                                }
                            }
                        }
                    }

                    // taxincome - based on influences
                    //todo this might calculate other galaxies, check before adding more galaxies
                    foreach (var ap in db.AccountPlanets.GroupBy(x=>x.Account).Select(x=> new {Acc = x.Key, Infl = x.Sum(y=>y.Influence + y.ShadowInfluence)})) {
                        ap.Acc.Credits += (int)Math.Round(ap.Infl * GlobalConst.InfluenceTaxIncome);
                    }

                    // kill structures you cannot support 
                    foreach (var owner in gal.Planets.Where(x => x.Account != null).Select(x=>x.Account).Distinct()) {
                        if (owner.Credits < 0) {
                            var upkeepStructs = owner.Planets.SelectMany(x => x.PlanetStructures).Where(x => !x.IsDestroyed && x.StructureType.UpkeepCost > 0).OrderByDescending(x => x.StructureType.UpkeepCost);
                            var structToKill = upkeepStructs.FirstOrDefault();
                            if (structToKill != null) {
                                structToKill.IsDestroyed = true;
                                owner.Credits += structToKill.StructureType.UpkeepCost;
                                db.Events.InsertOnSubmit(Global.CreateEvent("{0} on {1}'s planet {2} has been destroyed due to lack of upkeep", structToKill.StructureType.Name, owner, structToKill.Planet));
                            }
                        }
                    }


                    var oldOwner = planet.OwnerAccountID;
                    gal.Turn++;
                    db.SubmitChanges();


                    // give unclanned influence to clanned
                    if (GlobalConst.GiveUnclannedInfluenceToClanned)
                    {
                        db = new ZkDataContext();
                        planet = db.Planets.Single(x => x.PlanetID == planet.PlanetID);
                        foreach (
                            var faction in
                                planet.AccountPlanets.Where(x => x.Account.FactionID != null && x.Influence > 0).GroupBy(x => x.Account.FactionID))
                        {
                            var unclanned = faction.Where(x => x.Account.ClanID == null).ToList();
                            var clanned = faction.Where(x => x.Account.ClanID != null).ToList();
                            int unclannedInfluence = 0;
                            if (unclanned.Any() && clanned.Any() && (unclannedInfluence = unclanned.Sum(x => x.Influence)) > 0)
                            {
                                int influenceBonus = unclannedInfluence/clanned.Count();
                                foreach (var clannedEntry in clanned) clannedEntry.Influence += influenceBonus;
                                foreach (var unclannedEntry in unclanned) unclannedEntry.Influence = 0;
                            }
                        }
                        db.SubmitChanges();
                    }

                    // transfer ceasefire/alliance influences
                    if (ownerClan != null)
                    {
                        db = new ZkDataContext();
                        planet = db.Planets.Single(x => x.PlanetID == planet.PlanetID);

                        var ownerEntries = planet.AccountPlanets.Where(x => x.Influence > 0 && x.Account.ClanID == ownerClan.ClanID).ToList();
                        if (ownerEntries.Any())
                        {
                            foreach (
                                var clan in
                                    planet.AccountPlanets.Where(
                                        x =>
                                        x.Account.ClanID != null && x.Account.ClanID != ownerClan.ClanID && x.Influence > 0 &&
                                        x.Account.Clan.FactionID != ownerClan.FactionID).GroupBy(x => x.Account.Clan))
                            {
                                // get clanned influences of other than owner clans of different factions


                                var treaty = clan.Key.GetEffectiveTreaty(ownerClan);
                                if (treaty.AllyStatus == AllyStatus.Alliance ||
                                    (treaty.AllyStatus == AllyStatus.Ceasefire &&
                                     treaty.InfluenceGivenToSecondClanBalance < GlobalConst.CeasefireMaxInfluenceBalanceRatio))
                                {
                                    // if we are allied or ceasefired and influence balance < 150%, send ifnluence to owners
                                    var total = clan.Sum(x => x.Influence);
                                    var increment = total/ownerEntries.Count();
                                    foreach (var e in ownerEntries) e.Influence += increment;
                                    foreach (var e in clan) e.Influence = 0;

                                    var offer = db.TreatyOffers.SingleOrDefault(x => x.OfferingClanID == clan.Key.ClanID && x.TargetClanID == ownerClan.ClanID);
                                    if (offer == null) {
                                        offer = new TreatyOffer() { OfferingClanID = clan.Key.ClanID, TargetClanID = ownerClan.ClanID };
                                        db.TreatyOffers.InsertOnSubmit(offer);
                                    }
                                    offer.InfluenceGiven += total;

                                    db.Events.InsertOnSubmit(Global.CreateEvent("{0} gave {1} influence on {2} to clan {3} because of their treaty", clan.Key, total, planet, ownerClan));
                                }
                            }
                        }
                        db.SubmitChanges();
                   }
                    



                    db = new ZkDataContext(); // is this needed - attempt to fix setplanetownersbeing buggy
                    PlanetwarsController.SetPlanetOwners(db, sb);
                    gal = db.Galaxies.Single(x => x.IsDefault);

                    if (GlobalConst.ClanFreePlanets)
                    {
                        // give free planet to each clan with none here
                        foreach (var kvp in
                            sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.Account != null && x.Account.ClanID != null).GroupBy(
                                x => x.Account.ClanID))
                        {
                            var clan = db.Clans.Single(x => x.ClanID == kvp.Key);
                            var changed = false;
                            if (clan.Accounts.Sum(x => x.Planets.Count()) == 0)
                            {
                                var planetList =
                                    gal.Planets.Where(
                                        x => x.OwnerAccountID == null && !x.PlanetStructures.Any(y => y.StructureType.EffectIsVictoryPlanet == true)).
                                        Shuffle(); //pick planets which only have wormhole
                                if (planetList.Count > 0)
                                {
                                    var freePlanet = planetList[new Random().Next(planetList.Count)];
                                    foreach (var ac in kvp)
                                        db.AccountPlanets.InsertOnSubmit(new AccountPlanet()
                                                                         { PlanetID = freePlanet.PlanetID, AccountID = ac.AccountID, Influence = 501 });
                                    db.Events.InsertOnSubmit(Global.CreateEvent("{0} was awarded empty planet {1} {2}", clan, freePlanet, sb));
                                    changed = true;
                                }
                            }
                            if (changed)
                            {
                                db.SubmitChanges();
                                db = new ZkDataContext();
                                PlanetwarsController.SetPlanetOwners(db, sb);
                                gal = db.Galaxies.Single(x => x.IsDefault);
                            }
                        }
                    }

                    planet = gal.Planets.Single(x => x.Resource.InternalName == result.Map);
                    if (planet.OwnerAccountID != oldOwner && planet.OwnerAccountID != null)
                    {
                        text.AppendFormat("Congratulations!! Planet {0} was conquered by {1} !!  http://zero-k.info/PlanetWars/Planet/{2}\n",
                                          planet.Name,
                                          planet.Account.Name,
                                          planet.PlanetID);
                    }

                    try
                    {
                        // store history
                        foreach (var p in gal.Planets)
                        {
                            db.PlanetOwnerHistories.InsertOnSubmit(new PlanetOwnerHistory()
                                                                   {
                                                                       PlanetID = p.PlanetID,
                                                                       OwnerAccountID = p.OwnerAccountID,
                                                                       OwnerClanID = p.OwnerAccountID != null ? p.Account.ClanID : null,
                                                                       Turn = gal.Turn
                                                                   });

                            foreach (var pi in p.AccountPlanets.Where(x => x.Account.ClanID != null))
                            {
                                db.PlanetInfluenceHistories.InsertOnSubmit(new PlanetInfluenceHistory()
                                                                           {
                                                                               PlanetID = p.PlanetID,
                                                                               AccountID = pi.AccountID,
                                                                               ClanID = pi.Account.ClanID,
                                                                               Influence = pi.Influence + pi.ShadowInfluence,
                                                                               Turn = gal.Turn
                                                                           });
                            }
                        }

                        db.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        text.AppendLine("error saving history: " + ex.ToString());
                    }
                }

                foreach (var account in sb.SpringBattlePlayers.Select(x => x.Account))
                {
                    if (account.Level > orgLevels[account.AccountID])
                    {
                        try
                        {
                            var message = string.Format("Congratulations {0}! You just leveled up to level {1}. http://zero-k.info/Users/Detail/{2}",
                                                        account.Name,
                                                        account.Level,
                                                        account.AccountID);
                            text.AppendLine(message);
                            AuthServiceClient.SendLobbyMessage(account, message);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error sending level up lobby message: {0}", ex);
                        }
                    }
                }

                text.AppendLine(string.Format("View full battle details and demo at http://zero-k.info/Battles/Detail/{0}", sb.SpringBattleID));
                return text.ToString();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [WebMethod]
        public void SubmitStackTrace(ProgramType programType, string playerName, string exception, string extraData, string programVersion)
        {
            using (var db = new ZkDataContext())
            {
                var exceptionLog = new ExceptionLog
                                   {
                                       ProgramID = programType,
                                       Time = DateTime.UtcNow,
                                       PlayerName = playerName,
                                       ExtraData = extraData,
                                       Exception = exception,
                                       ExceptionHash = new Hash(exception).ToString(),
                                       ProgramVersion = programVersion,
                                       RemoteIP = GetUserIP()
                                   };
                db.ExceptionLogs.InsertOnSubmit(exceptionLog);
                db.SubmitChanges();
            }
        }


        [WebMethod]
        public bool VerifyAccountData(string login, string password)
        {
            var acc = AuthServiceClient.VerifyAccountPlain(login, password);
            if (acc == null) return false;
            return true;
        }

        string GetUserIP()
        {
            var ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }


        public class BattlePlayerResult
        {
            public int AccountID;
            public int AllyNumber;
            public List<PlayerAward> Awards;
            public string CommanderType;
            public bool IsIngameReady;
            public bool IsSpectator;
            public bool IsVictoryTeam;
            public int? LoseTime;
            public int Rank;
            public List<PlayerStats> Stats;

            public class PlayerAward
            {
                public string Award;
                public string Description;
            }

            public class PlayerStats
            {
                public string Key;
                public double Value;
            }
        }

        public class BattleResult
        {
            public int Duration;
            public string EngineBattleID;
            public string EngineVersion;
            public DateTime? IngameStartTime;
            public bool IsBots;
            public bool IsMission;
            public string Map;
            public string Mod;
            public string ReplayName;
            public DateTime StartTime;
            public string Title;
        }

        public class BattleStartSetupPlayer
        {
            public int AccountID;
            public int AllyTeam;
            public bool IsSpectator;
            public int SpringPlayerID;
        }

        public class EloInfo
        {
            public double Elo = 1500;
            public double Weight = 1;
        }

        public class ScriptMissionData
        {
            public List<string> ManualDependencies;
            public string MapName;
            public string ModTag;
            public string Name;
            public string StartScript;
        }

        public class SpringBattleStartSetup
        {
            public List<ScriptKeyValuePair> ModOptions = new List<ScriptKeyValuePair>();
            public List<UserCustomParameters> UserParameters = new List<UserCustomParameters>();

            public class ScriptKeyValuePair
            {
                public string Key;
                public string Value;
            }

            public class UserCustomParameters
            {
                public int AccountID;
                public List<ScriptKeyValuePair> Parameters = new List<ScriptKeyValuePair>();
            }
        }
    }

    public class RecommendedMapResult
    {
        public string MapName;
        public string Message;
    }

    public class BalanceTeamsResult
    {
        public List<AccountTeam> BalancedTeams = new List<AccountTeam>();
        public List<BotTeam> Bots = new List<BotTeam>();
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
        public string BotName;
        public int TeamID;
    }


    public enum AutohostMode
    {
        Planetwars = 1,
        Game1v1 = 2,
        GameTeams = 3,
        GameFFA = 4,
        GameChickens = 5
    }
}