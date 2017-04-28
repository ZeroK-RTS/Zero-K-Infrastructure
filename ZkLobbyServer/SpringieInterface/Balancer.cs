using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PlasmaShared;
using ZkData;
using ZkLobbyServer;
using Ratings;

namespace ZeroKWeb.SpringieInterface
{
    public class Balancer
    {
        public enum BalanceMode
        {
            Normal,
            Party, 
            ClanWise,
            FactionWise
        }

        const double MaxCbalanceDifference = 70;
        const double MaxTeamSizeDifferenceRatio = 2;
        readonly List<BalanceTeam> teams = new List<BalanceTeam>();

        List<BalanceItem> balanceItems;

        double bestStdDev = double.MaxValue;
        List<BalanceTeam> bestTeams;
        long iterationsChecked;
        int maxTeamSize;

        public static BalanceTeamsResult BalanceTeams(LobbyHostingContext context, bool isGameStart, int? allyCount, bool? clanWise)
        {
            var playerCount = context.Players.Count(x => !x.IsSpectator);


            if (clanWise == null && (context.Mode == AutohostMode.Teams)) clanWise = true;


            var res = PerformBalance(context, isGameStart, allyCount, clanWise);

            if (context.Mode != AutohostMode.None && context.Mode != AutohostMode.GameChickens)
            {
                if (isGameStart)
                {
                    // dont allow to start alone
                    if (playerCount <= 1)
                    {
                        return new BalanceTeamsResult
                        {
                            CanStart = false,
                            Message = "Sorry this room type needs more human players, open cooperative battle to fight against bots."
                        };
                    }
                }
            }
            if (isGameStart) VerifySpecCheaters(context, res);

            return res;
        }


        public static List<BalanceTeam> CloneTeams(List<BalanceTeam> t)
        {
            return new List<BalanceTeam>(t.Select(x => x.Clone()));
        }

        public static double GetTeamsDifference(List<BalanceTeam> t)
        {
            if (t.Count == 2) return Math.Abs(t[0].AvgElo - t[1].AvgElo);
            var min = double.MaxValue;
            var max = double.MinValue;
            foreach (var team in t)
            {
                if (team.AvgElo > max) max = team.AvgElo;
                if (team.AvgElo < min) min = team.AvgElo;
            }
            return max - min;
        }


        /// <summary>
        ///     The function that actually moves players arounds
        /// </summary>
        /// <param name="teamCount"></param>
        /// <param name="mode"></param>
        /// <param name="b"></param>
        /// <param name="unmovablePlayers"></param>
        /// <returns></returns>
        BalanceTeamsResult LegacyBalance(int teamCount, BalanceMode mode, LobbyHostingContext b, params List<Account>[] unmovablePlayers)
        {
            var ret = new BalanceTeamsResult();

            if (b.IsMatchMakerGame) mode = BalanceMode.Party; // override, for matchmaker mode is always party
            
            try
            {
                ret.CanStart = true;
                ret.Players = b.Players.ToList();

                var db = new ZkDataContext();
                var nonSpecList = b.Players.Where(y => !y.IsSpectator).Select(y => (int?)y.LobbyID).ToList();
                var accs = db.Accounts.Where(x => nonSpecList.Contains(x.AccountID)).ToList();
                if (accs.Count < 1)
                {
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

                teams.Clear();
                for (var i = 0; i < teamCount; i++)
                {
                    var team = new BalanceTeam();
                    teams.Add(team);
                    if (unmovablePlayers != null && unmovablePlayers.Length > i)
                    {
                        var unmovables = unmovablePlayers[i];
                        team.AddItem(new BalanceItem(b.IsMatchMakerGame, unmovablePlayers[i].ToArray()) { CanBeMoved = false });
                        accs.RemoveAll(x => unmovables.Any(y => y.AccountID == x.AccountID));
                    }
                }

                balanceItems = new List<BalanceItem>();
                if (mode == BalanceMode.Party)
                {
                    var clanGroups = accs.GroupBy(x => b.Players.First(p => p.Name == x.Name).PartyID ?? x.AccountID).ToList();
                    if (teamCount > clanGroups.Count() || clanGroups.Any(x => x.Count() > maxTeamSize)) mode = BalanceMode.Normal;
                    else balanceItems.AddRange(clanGroups.Select(x => new BalanceItem(b.IsMatchMakerGame, x.ToArray())));
                }

                if (mode == BalanceMode.ClanWise)
                {
                    var clanGroups = accs.GroupBy(x => b.Players.First(p => p.Name == x.Name).PartyID ?? x.ClanID ?? x.AccountID).ToList();
                    if (teamCount > clanGroups.Count() || clanGroups.Any(x => x.Count() > maxTeamSize)) mode = BalanceMode.Normal;
                    else balanceItems.AddRange(clanGroups.Select(x => new BalanceItem(b.IsMatchMakerGame, x.ToArray())));
                }
                if (mode == BalanceMode.FactionWise)
                {
                    balanceItems.Clear();
                    var factionGroups = accs.GroupBy(x => x.FactionID ?? x.AccountID).ToList();
                    balanceItems.AddRange(factionGroups.Select(x => new BalanceItem(b.IsMatchMakerGame, x.ToArray())));
                }

                if (mode == BalanceMode.Normal)
                {
                    balanceItems.Clear();
                    balanceItems.AddRange(accs.Select(x => new BalanceItem(b.IsMatchMakerGame, x)));
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
                var sizesWrong = maxSize / (double)minSize > MaxTeamSizeDifferenceRatio;

                // cbalance failed, rebalance using normal
                if (mode == BalanceMode.ClanWise && (bestTeams == null || GetTeamsDifference(bestTeams) > MaxCbalanceDifference || sizesWrong)) return new Balancer().LegacyBalance(teamCount, BalanceMode.Normal, b, unmovablePlayers);
                // cbalance failed, rebalance using normal

                if (sizesWrong && mode == BalanceMode.FactionWise)
                {
                    var fallback = new Balancer().LegacyBalance(teamCount, BalanceMode.ClanWise, b, null);
                    fallback.Message += "\nWarning: STANDARD TEAM BALANCE USED, PlanetWars not possible with those teams, too many from one faction";
                    return fallback; // fallback standard balance if PW balance fails
                    /*ret.CanStart = false;
                    ret.Message = string.Format("Failed to balance - too many people from same faction");
                    return ret;*/
                }

                if (unmovablePlayers != null && unmovablePlayers.Length > 0)
                {
                    var minElo = bestTeams.Min(x => x.AvgElo);
                    var maxElo = bestTeams.Max(x => x.AvgElo);
                    if (maxElo - minElo > GlobalConst.MaxPwEloDifference)
                    {
                        var fallback = new Balancer().LegacyBalance(teamCount, BalanceMode.ClanWise, b, null);
                        fallback.Message +=
                            "\nWarning: STANDARD TEAM BALANCE USED, PlanetWars not possible with those teams, too many from one faction";
                        return fallback; // fallback standard balance if PW balance fails
                        /*
                        ret.CanStart = false;
                        ret.Message = string.Format("Team difference is too big - win chance {0}% - spectate some or wait for more people",
                                                    Utils.GetWinChancePercent(maxElo - minElo));
                        return ret;*/
                    }
                }

                if (bestTeams == null)
                {
                    ret.CanStart = false;
                    ret.Message =
                        string.Format(
                            "Failed to balance {0} - too many people from same clan or faction (in teams game you can try !random and !forcestart)");
                    return ret;
                }
                if (unmovablePlayers == null || unmovablePlayers.Length == 0) bestTeams = bestTeams.Shuffle(); // permute when not unmovable players present

                var text = "( ";

                var lastTeamElo = 0.0;
                var allyNum = 0;
                foreach (var team in bestTeams)
                {
                    if (allyNum > 0) text += " : ";
                    text += string.Format("{0}", (allyNum + 1));
                    if (allyNum > 0) text += string.Format("={0}%)", Utils.GetWinChancePercent(lastTeamElo - team.AvgElo));
                    lastTeamElo = team.AvgElo;

                    foreach (var u in team.Items.SelectMany(x => x.LobbyId)) ret.Players.Single(x => x.LobbyID == u).AllyID = allyNum;
                    allyNum++;
                }
                text += ")";

                ret.Message = string.Format(
                    "{0} players balanced {2} to {1} teams {3}. {4} combinations checked, spent {5}ms of CPU time",
                    bestTeams.Sum(x => x.Count),
                    teamCount,
                    mode,
                    text,
                    iterationsChecked,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                ret.Message = ex.ToString();
                ret.CanStart = false;
            }
            return ret;
        }

        /// <summary>
        ///     Calls <see cref="LegacyBalance" /> with the appropriate parameters depending on game settings and conditions
        /// </summary>
        /// <param name="isGameStart">
        ///     If true and <see cref="AutohostMode" /> is none, do nothing (i.e. don't autobalance custom
        ///     rooms at start)
        /// </param>
        /// <param name="allyCount"></param>
        /// <param name="clanWise"></param>
        /// <param name="config"></param>
        /// <remarks>Also removes bots from team games, and tells people to add bots to a chicken game if absent</remarks>
        static BalanceTeamsResult PerformBalance(
            LobbyHostingContext context,
            bool isGameStart,
            int? allyCount,
            bool? clanWise)
        {
            var res = new BalanceTeamsResult();
            var mode = context.Mode;

            using (var db = new ZkDataContext())
            {
                switch (mode)
                {
                    case AutohostMode.None:
                        {
                            if (!isGameStart) res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise == true ? BalanceMode.ClanWise : BalanceMode.Normal, context);
                        }
                        break;
                    case AutohostMode.Teams:
                    case AutohostMode.Game1v1:
                        {
                            res = new Balancer().LegacyBalance(allyCount ?? 2, clanWise == false ? BalanceMode.Normal : BalanceMode.ClanWise, context);
                            res.DeleteBots = true;
                        }
                        break;
                    
                    case AutohostMode.GameChickens:
                        {
                            res.Players = context.Players.ToList();
                            res.Bots = context.Bots.ToList();
                            foreach (var p in res.Players) p.AllyID = 0;
                            foreach (var b in res.Bots) b.AllyID = 1;

                            // add chickens via modoptions hackish thingie
                            string chickBot = null;
                            if (context.ModOptions?.TryGetValue("chickenailevel", out chickBot) == true && !string.IsNullOrEmpty(chickBot) && chickBot != "none")
                            {
                                res.Bots.RemoveAll(x => x.BotAI.StartsWith("Chicken:"));
                                res.Bots.Add(new BotTeam() { AllyID = 1, BotName = "default_Chicken", BotAI = chickBot });
                            }

                            if (!res.Bots.Any() && res.Players.Count > 0)
                            {
                                //res.Message = "Add some bot (computer player) as your enemy. Use button on bottom left. Chicken or CAI is recommended.";
                                var map = db.Resources.FirstOrDefault(x => x.InternalName == context.Map);
                                if (map?.MapIsChickens == true) res.Bots.Add(new BotTeam() { AllyID = 1, BotName = "default_Chicken", BotAI = "Chicken: Normal", });
                                else
                                {
                                    for (int i =1; i<= res.Players.Where(x => !x.IsSpectator).Count(); i++) res.Bots.Add(new BotTeam() { AllyID = 1, BotName = "cai" + i, BotAI = "CAI", });
                                }
                                res.Message = "Adding computer AI player for you";
                            }
                        }
                        break;
                    case AutohostMode.GameFFA:
                        {
                            res.DeleteBots = true;
                            var map = db.Resources.Single(x => x.InternalName == context.Map);
                            if (map.MapFFAMaxTeams != null)
                            {
                                res = new Balancer().LegacyBalance(
                                    allyCount ?? map.MapFFAMaxTeams.Value,
                                    clanWise == false ? BalanceMode.Normal : BalanceMode.ClanWise,
                                    context);
                            }
                            else
                            {
                                res = new Balancer().LegacyBalance(
                                    allyCount ?? map.MapFFAMaxTeams ?? 8,
                                    clanWise == false ? BalanceMode.Normal : BalanceMode.ClanWise,
                                    context);
                            }
                            return res;
                        }
                    case AutohostMode.Planetwars:

                        return new Balancer().PlanetwarsBalance(context);
                }
                return res;
            }
        }

        BalanceTeamsResult PlanetwarsBalance(LobbyHostingContext context)
        {
            var res = new BalanceTeamsResult();
            res.CanStart = true;
            res.DeleteBots = true;

            using (var db = new ZkDataContext())
            {
                res.Message = "";
                var planet = db.Galaxies.Single(x => x.IsDefault).Planets.First(x => x.Resource.InternalName == context.Map);

                res.Players = context.Players;

                // bots game
                var cnt = 0;
                if (planet.PlanetStructures.Any(x => !string.IsNullOrEmpty(x.StructureType.EffectBots)))
                {
                    foreach (var b in planet.PlanetStructures.Select(x => x.StructureType).Where(x => !string.IsNullOrEmpty(x.EffectBots))) res.Bots.Add(new BotTeam { AllyID = 1, BotAI = b.EffectBots, BotName = "Aliens" + cnt++ });

                    res.Message += "This planet is infested by aliens, fight for your survival";
                    return res;
                }
                
                return res;
            }
        }

      
        /// <summary>
        ///     Gets the best balance (lowest standard deviation between teams)
        /// </summary>
        void RecursiveBalance(int itemIndex)
        {
            if (iterationsChecked > 2000000) return;

            if (itemIndex < balanceItems.Count)
            {
                var item = balanceItems[itemIndex];

                if (item.CanBeMoved)
                {
                    foreach (var team in teams)
                    {
                        if (team.Count + item.Count <= maxTeamSize)
                        {
                            team.AddItem(item);
                            RecursiveBalance(itemIndex + 1);
                            team.RemoveItem(item);
                        }
                    }
                }
                else RecursiveBalance(itemIndex + 1);
            }
            else
            {
                // end of recursion
                iterationsChecked++;
                var stdDev = GetTeamsDifference(teams);
                if (stdDev < bestStdDev)
                {
                    bestStdDev = stdDev;
                    bestTeams = CloneTeams(teams);
                }
            }
        }

        static void SpecPlayerOnCondition(PlayerTeam player, Account account, string userMessage, ZkLobbyServer.ZkLobbyServer server)
        {
            player.IsSpectator = true;
            server.GhostPm(account.Name, userMessage);
        }

        /// <summary>
        ///     Makes <see cref="Springie" /> print a message if two or more people have the same IP
        /// </summary>
        /// <param name="context"></param>
        /// <param name="res">The <see cref="BalanceTeamsResult" /> to write the message to</param>
        static void VerifySpecCheaters(LobbyHostingContext context, BalanceTeamsResult res)
        {
            try
            {
                // find specs with same IP as some player and kick them
                using (var db = new ZkDataContext())
                {
                    var ids = context.Players.Select(y => (int?)y.LobbyID).ToList();
                    var ipByLobbyID = db.Accounts.Where(x => ids.Contains(x.AccountID))
                        .ToDictionary(x => x.AccountID, x => x.AccountIPs.OrderByDescending(y => y.LastLogin).Select(y => y.IP).FirstOrDefault());
                    // lobbyid -> ip mapping

                    foreach (var grp in context.Players.GroupBy(x => ipByLobbyID[x.LobbyID]).Where(x => x.Count() > 1)) res.Message +=
                        $"\nThese people are in same location: {string.Join(", ", grp.Select(x => x.Name))}";
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error checking speccheaters: {0}", ex);
            }
        }

        public class BalanceItem
        {
            public readonly int Count;
            public readonly double EloSum;
            public readonly List<int> LobbyId;
            public bool CanBeMoved = true;

            public BalanceItem(bool isMatchMaker, params Account[] accounts)
            {
                LobbyId = accounts.Select(x => x.AccountID).ToList();
                Count = accounts.Length;

                if (RatingSystems.DisableRatingSystems)
                {
                    EloSum = isMatchMaker ? accounts.Sum(x => x.EffectiveMmElo) : accounts.Sum(x => x.EffectiveElo);
                }
                else
                {
                    RatingCategory category = isMatchMaker ? RatingCategory.MatchMaking : RatingCategory.Casual;
                    EloSum = accounts.Sum(x => x.GetRating(category).Elo);
                }
            }
        }

        public class BalanceTeam
        {
            public List<BalanceItem> Items = new List<BalanceItem>();
            public double AvgElo { get; private set; }
            public int Count { get; private set; }
            public double EloSum { get; private set; }

            public void AddItem(BalanceItem item)
            {
                Items.Add(item);
                EloSum += item.EloSum;
                Count += item.Count;
                if (Count > 0) AvgElo = EloSum / Count;
                else AvgElo = 0;
            }

            public BalanceTeam Clone()
            {
                var clone = new BalanceTeam();
                clone.Items = new List<BalanceItem>(Items);
                clone.AvgElo = AvgElo;
                clone.EloSum = EloSum;
                clone.Count = Count;
                return clone;
            }

            public void RemoveItem(BalanceItem item)
            {
                Items.Remove(item);
                EloSum -= item.EloSum;
                Count -= item.Count;
                if (Count > 0) AvgElo = EloSum / Count;
                else AvgElo = 0;
            }
        }
    }
}
