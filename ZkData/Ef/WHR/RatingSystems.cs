using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZkData;
using System.Data.Entity;

namespace Ratings
{
    public class RatingSystems
    {
        public static Dictionary<RatingCategory, WholeHistoryRating> whr = new Dictionary<RatingCategory, WholeHistoryRating>();

        public static readonly IEnumerable<RatingCategory> ratingCategories = Enum.GetValues(typeof(RatingCategory)).Cast<RatingCategory>();

        private static HashSet<int> processedBattles = new HashSet<int>();

        public static bool Initialized { get; private set; }

        private static object processingLock = new object();

        public static void Init()
        {
            Initialized = false;
            ratingCategories.ForEach(category => whr[category] = new WholeHistoryRating(category));

            Task.Factory.StartNew(() => {
                lock (processingLock)
                {
                    try
                    {
                        using (ZkDataContext data = new ZkDataContext())
                        {
                            for (int year = 10; year > 0; year--)
                            {
                                DateTime minStartTime = DateTime.Now.AddYears(-year);
                                DateTime maxStartTime = DateTime.Now.AddYears(-year + 1);
                                foreach (SpringBattle b in data.SpringBattles
                                        .Where(x => x.StartTime > minStartTime && x.StartTime < maxStartTime)
                                        .Include(x => x.ResourceByMapResourceID)
                                        .Include(x => x.SpringBattlePlayers)
                                        .Include(x => x.SpringBattleBots)
                                        .AsNoTracking()
                                        .OrderBy(x => x.StartTime))
                                {
                                    ProcessBattle(b);
                                }
                            }
                            Initialized = true;
                            whr.Values.ForEach(w => w.UpdateRatings());
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("WHR: Error reading battles from DB" + ex);
                    }
                }
            });
        }


        public static IEnumerable<IRatingSystem> GetRatingSystems()
        {
            return whr.Values;
        }

        public static IRatingSystem GetRatingSystem(RatingCategory category)
        {
            if (!whr.ContainsKey(category))
            {
                Trace.TraceWarning("WHR: Unknown category " + category + " " + new StackTrace());
                return whr[RatingCategory.MatchMaking];
            }
            return whr[category];
        }

        private static int latestBattle;

        public static void ProcessResult(SpringBattle battle)
        {
            if (!Initialized) return;
            ProcessBattle(battle);
        }

        private static void ProcessBattle(SpringBattle battle)
        {
            lock (processingLock)
            {
                int battleID = -1;
                try
                {
                    battleID = battle.SpringBattleID;
                    if (processedBattles.Contains(battleID)) return;
                    processedBattles.Add(battleID);
                    ratingCategories.Where(c => IsCategory(battle, c)).ForEach(c => whr[c].ProcessBattle(battle));
                    latestBattle = battleID;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("WHR: Error processing battle (B" + battleID + ")" + ex);
                }
            }
        }

        private static Dictionary<int, Tuple<int, int, int>> factionCache = new Dictionary<int, Tuple<int, int, int>>();

        public static Tuple<int, int> GetPlanetwarsFactionStats(int factionID)
        {
            try
            {
                int count, skill;
                if (!factionCache.ContainsKey(factionID) || factionCache[factionID].Item1 != latestBattle)
                {
                    var maxAge = DateTime.UtcNow.AddDays(-7);
                    IEnumerable<Account> accounts;
                    var rating = RatingCategory.Planetwars;
                    if (GlobalConst.PlanetWarsMode == PlanetWarsModes.PreGame)
                    {
                        rating = RatingCategory.MatchMaking;
                        accounts = GetRatingSystem(rating).GetTopPlayers(int.MaxValue, x => x.LastLogin > maxAge && x.FactionID == factionID);
                    }
                    else
                    {
                        accounts = GetRatingSystem(rating).GetTopPlayers(int.MaxValue, x => x.PwAttackPoints > 0 && x.FactionID == factionID);
                    }
                    count = accounts.Count();
                    skill = count > 0 ? (int)Math.Round(accounts.Average(x => x.GetRating(rating).Elo)) : 1500;
                    factionCache[factionID] = new Tuple<int, int, int>(latestBattle, count, skill);
                }
                count = factionCache[factionID].Item2;
                skill = factionCache[factionID].Item3;
                return new Tuple<int, int>(count, skill);
            }catch(Exception ex)
            {
                Trace.TraceError("WHR failed to calculate faction stats " + ex);
                return new Tuple<int, int>(-1, -1);
            }
        }

        public static int ConvertDateToDays(DateTime date)
        {
            return (int)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays / 1);
        }
        public static DateTime ConvertDaysToDate(int days)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(days);
        }

        private static bool IsCategory(SpringBattle battle, RatingCategory category)
        {
            int battleID = -1;
            try
            {
                if (battle.HasBots) return false;
                battleID = battle.SpringBattleID;
                switch (category)
                {
                    case RatingCategory.Casual:
                        return battle.ApplicableRatings.HasFlag(RatingCategoryFlags.Casual);
                    case RatingCategory.MatchMaking:
                        return battle.ApplicableRatings.HasFlag(RatingCategoryFlags.MatchMaking);
                    case RatingCategory.Planetwars:
                        return battle.ApplicableRatings.HasFlag(RatingCategoryFlags.Planetwars);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("WHR: Error while checking battle category (B" + battleID + ")" + ex);
            }
            return false;
        }
    }
    
    public enum RatingCategory
    {
        Casual = 1, MatchMaking = 2, Planetwars = 4
    }
    [Flags]
    public enum RatingCategoryFlags
    {
        Casual = 1, MatchMaking = 2, Planetwars = 4
    }
}
