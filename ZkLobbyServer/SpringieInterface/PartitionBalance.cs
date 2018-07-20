using PlasmaShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZkData;
using static ZeroKWeb.SpringieInterface.Balancer;

namespace ZeroKWeb.SpringieInterface
{
    //Optimized balancing for two teams
    class DualBalance
    {
        public class PlayerItem
        {
            public int Account;
            public double Elo;
            public int? Clan;
            public int? Party;

            public PlayerItem(int AccountID, double Elo, string Clan, int? PartyID)
            {
                Account = AccountID;
                this.Elo = Elo;
                if (!string.IsNullOrEmpty(Clan)) this.Clan = Clan.GetHashCode();
                Party = PartyID;
            }
        }


        private class Subset : IComparable
        {
            public double Sum;
            public UInt64 Elements;

            public int CompareTo(object o)
            {
                return Sum.CompareTo(((Subset)o).Sum);
            }
        }


        //Generates all 2^N Subsets of a set of numbers
        private static List<Subset> generateAllSubsets(List<double> nums)
        {

            List<Subset> ret = new List<Subset>();
            double sum1 = 0;
            double sum2 = nums.Sum();
            UInt64 all = (1UL << nums.Count) - 1;
            UInt64 prevI = 0;
            UInt64 change;
            for (UInt64 i = 0; i < (1UL << (nums.Count - 1)); i++)
            {
                change = (i ^ prevI);
                int j = 0;
                while (change > 0)
                {
                    if ((change & 0x1) != 0)
                    {
                        if ((prevI & 0x1) == 0)
                        {
                            sum1 += nums[j];
                            sum2 -= nums[j];
                        }
                        else
                        {
                            sum2 += nums[j];
                            sum1 -= nums[j];
                        }
                    }
                    j++;
                    prevI >>= 1;
                    change >>= 1;
                }
                prevI = i;
                ret.Add(new Subset() { Sum = sum1, Elements = i });
                ret.Add(new Subset() { Sum = sum2, Elements = all ^ i });
            }
            return ret;
        }


        public static DualBalanceResult Balance(BalanceMode mode, ICollection<PlayerItem> players)
        {
            List<PlayerItem> unmodifiedPlayers = players.ToList();
            List<List<int>> playerGroups = new List<List<int>>();
            List<double> groupSkill = new List<double>();
            players.ForEach(x => x.Elo += 1e6); //Purple for everyone
            if (players.Count % 2 != 0)
            {
                players.Add(new PlayerItem(-1, players.Average(x => x.Elo), null, null));
            }
            int maxTeamSize = players.Count / 2;

            if (mode == BalanceMode.Party)
            {
                var partyGroups = players.GroupBy(x => x.Party ?? x.Account).ToList();
                if (2 > partyGroups.Count() || partyGroups.Any(x => x.Count() > maxTeamSize))
                {
                    //Party too big, fall through
                    mode = BalanceMode.Normal;
                }
                else
                {
                    playerGroups = partyGroups.Select(x => x.Select(p => p.Account).ToList()).ToList();
                    groupSkill = partyGroups.Select(x => x.Sum(g => g.Elo)).ToList();
                }
            }

            if (mode == BalanceMode.ClanWise)
            {
                var clanGroups = players.GroupBy(x => x.Party ?? x.Clan ?? x.Account).ToList();
                if (2 > clanGroups.Count() || clanGroups.Any(x => x.Count() > maxTeamSize))
                {
                    //Clan or party too big, fall through
                    mode = BalanceMode.Normal;
                }
                else
                {
                    playerGroups = clanGroups.Select(x => x.Select(p => p.Account).ToList()).ToList();
                    groupSkill = clanGroups.Select(x => x.Sum(g => g.Elo)).ToList();
                }
            }

            if (mode == BalanceMode.Normal)
            {
                playerGroups = players.Select(x => new List<int>() { x.Account }).ToList();
                groupSkill = players.Select(x => x.Elo).ToList();
            }

            //Find optimal partitioning
            var sw = new Stopwatch();
            sw.Start();

            double sum = groupSkill.Sum();
            List<Subset> firstList = generateAllSubsets(groupSkill.Where((x, i) => i < maxTeamSize).ToList());
            List<Subset> secondList = generateAllSubsets(groupSkill.Where((x, i) => i >= maxTeamSize).ToList());

            firstList.Sort();
            secondList.Sort();
            
            int a = 0;
            int b = secondList.Count - 1;
            double bestDiff = Double.MaxValue;
            UInt64 best = 0;

            while (a < firstList.Count && b >= 0)
            {
                while (b >= 0 && firstList[a].Sum + secondList[b].Sum > sum / 2)
                {
                    b--;
                }
                if (b >= 0 && sum - 2 * (firstList[a].Sum + secondList[b].Sum) < bestDiff)
                {
                    bestDiff = sum - 2 * (firstList[a].Sum + secondList[b].Sum);
                    best = firstList[a].Elements | (secondList[b].Elements << (firstList.Count));
                }
                a++;
            }

            sw.Stop();

            //Return results

            if (mode == BalanceMode.ClanWise && (bestDiff > MaxCbalanceDifference))
            {
                //Not very balanced, fall back
                return Balance(BalanceMode.Normal, unmodifiedPlayers);
            }

            DualBalanceResult ret = new DualBalanceResult()
            {
                EloDifference = bestDiff / maxTeamSize,
                Players = players.Select(x => new PlayerTeam()
                {
                    LobbyID = x.Account
                }).ToList()
            };
            
            int j = 0;
            while (j < players.Count)
            {
                if ((best & 0x1) == 0)
                {
                    ret.Players.Where(p => playerGroups[j].Contains(p.LobbyID)).ForEach(x => x.AllyID = 0);
                }
                else
                {
                    ret.Players.Where(p => playerGroups[j].Contains(p.LobbyID)).ForEach(x => x.AllyID = 1);
                }
                j++;
                best >>= 1;
            }
            

            //Make that nice message

            var text = string.Format("( ( 1={0}%) : 2={1}%))", (int)Math.Round((1.0 / (1.0 + Math.Pow(10, ret.EloDifference / 400.0))) * 100.0), (int)Math.Round((1.0 / (1.0 + Math.Pow(10, -ret.EloDifference / 400.0))) * 100.0));
            
            ret.Message = string.Format(
                "{0} players balanced {2} to {1} teams {3}. {4} combinations checked, spent {5}ms of CPU time",
                unmodifiedPlayers.Count,
                2,
                mode,
                text,
                (1UL << maxTeamSize),
                sw.ElapsedMilliseconds);

            return ret;
        }

        public static BalanceTeamsResult BalanceInterface(int teamCount, BalanceMode mode, LobbyHostingContext b, params List<Account>[] unmovablePlayers)
        {
            if (teamCount != 2)
            {
                Trace.TraceWarning("DualBalance called with invalid number of teams: " + teamCount);
                return new Balancer().LegacyBalance(teamCount, mode, b, unmovablePlayers);
            }
            if (unmovablePlayers.Length > 0)
            {
                Trace.TraceWarning("DualBalance called with too many unmovable players: " + unmovablePlayers.Length);
                return new Balancer().LegacyBalance(teamCount, mode, b, unmovablePlayers);
            }
            if (mode == BalanceMode.FactionWise)
            {
                Trace.TraceWarning("DualBalance called with FactionWise balance mode, which is unsupported");
                return new Balancer().LegacyBalance(teamCount, mode, b, unmovablePlayers);
            }

            if (b.IsMatchMakerGame) mode = BalanceMode.Party;

            BalanceTeamsResult ret = new BalanceTeamsResult();

            ret.CanStart = true;
            ret.Players = b.Players.ToList();

            List<PlayerItem> players = new List<PlayerItem>();
            using (var db = new ZkDataContext())
            {
                var nonSpecList = b.Players.Where(y => !y.IsSpectator).Select(y => (int?)y.LobbyID).ToList();
                var accs = db.Accounts.Where(x => nonSpecList.Contains(x.AccountID)).ToList();
                if (accs.Count < 1)
                {
                    ret.CanStart = false;
                    return ret;
                }
                players = b.Players.Where(y => !y.IsSpectator).Select(x => new PlayerItem(x.LobbyID, accs.First(a => a.AccountID == x.LobbyID).GetRating(b.ApplicableRating).RealElo, x.Clan, x.PartyID)).ToList();
            }

            var dualResult = Balance(mode, players);
            dualResult.Players.ForEach(r => ret.Players.Where(x => x.LobbyID == r.LobbyID).ForEach(x => x.AllyID = r.AllyID));
            ret.Message = dualResult.Message;
            return ret;
        }
    }


    class DualBalanceResult
    {
        public List<PlayerTeam> Players = new List<PlayerTeam>();
        public double EloDifference;
        public string Message;
    }
}
