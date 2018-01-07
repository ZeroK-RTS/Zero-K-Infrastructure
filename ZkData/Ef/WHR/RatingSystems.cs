using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZkData;
using System.Data.Entity;
using System.Threading;

namespace Ratings
{
    public class RatingSystems
    {
        public static readonly IEnumerable<RatingCategory> ratingCategories = Enum.GetValues(typeof(RatingCategory)).Cast<RatingCategory>();

        private static RatingSystems handler;

        public static bool Initialized { get; private set; } = false;

        public static void Init()
        {
            handler = new RatingSystems(false);
        }

        public static void ReinitializeRatingSystems()
        {
            Trace.TraceInformation("Reinitializing rating systems...");
            Task.Factory.StartNew(() => {
                handler = new RatingSystems(true);
                Trace.TraceInformation("Ratings have been recalculated!");
            });
        }

        public static IEnumerable<IRatingSystem> GetRatingSystems()
        {
            return handler.whr.Values;
        }

        public static IRatingSystem GetRatingSystem(RatingCategory category)
        {
            if (handler == null) return null;
            return handler.GetRatingSystemInternal(category);
        }
        

        public static void ProcessResult(SpringBattle battle)
        {
            if (!Initialized) return;
            handler.ProcessBattle(battle);
        }
        
        public static Tuple<int, int> GetPlanetwarsFactionStats(int factionID)
        {
            return handler.GetPlanetwarsFactionStatsInternal(factionID);
        }

        public static int ConvertDateToDays(DateTime date)
        {
            return (int)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays / 1);
        }
        public static DateTime ConvertDaysToDate(int days)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(days);
        }


        private Dictionary<RatingCategory, WholeHistoryRating> whr = new Dictionary<RatingCategory, WholeHistoryRating>();

        private HashSet<int> processedBattles = new HashSet<int>();

        private object processingLock = new object();

        private RatingSystems(bool waitForCompleteInitialization)
        {
            ratingCategories.ForEach(category => whr[category] = new WholeHistoryRating(category));

            Action initBattles = () =>
            {
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
                            if (waitForCompleteInitialization)
                            {
                                SemaphoreSlim completedUpdates = new SemaphoreSlim(0);
                                whr.Values.ForEach(w => w.UpdateRatings(() => completedUpdates.Release()));
                                whr.Values.ForEach(w => completedUpdates.Wait());
                            }
                            else
                            {
                                whr.Values.ForEach(w => w.UpdateRatings());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("WHR: Error reading battles from DB" + ex);
                    }
                }
            };
            if (waitForCompleteInitialization) initBattles.Invoke();
            else Task.Factory.StartNew(initBattles);
        }
        

        private IRatingSystem GetRatingSystemInternal(RatingCategory category)
        {
            if (!whr.ContainsKey(category))
            {
                Trace.TraceWarning("WHR: Unknown category " + category + " " + new StackTrace());
                return whr[RatingCategory.MatchMaking];
            }
            return whr[category];
        }

        private int latestBattle;

        private void ProcessBattle(SpringBattle battle)
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

        private Dictionary<int, Tuple<int, int, int>> factionCache = new Dictionary<int, Tuple<int, int, int>>();

        private Tuple<int, int> GetPlanetwarsFactionStatsInternal(int factionID)
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
            }
            catch (Exception ex)
            {
                Trace.TraceError("WHR failed to calculate faction stats " + ex);
                return new Tuple<int, int>(-1, -1);
            }
        }

        private bool IsCategory(SpringBattle battle, RatingCategory category)
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
