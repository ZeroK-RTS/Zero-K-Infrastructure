using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZkData;
using System.Data.Entity;
using PlasmaShared;
using LobbyClient;

namespace Ratings
{
    public class RatingSystems
    {
        public static Dictionary<RatingCategory, WholeHistoryRating> whr = new Dictionary<RatingCategory, WholeHistoryRating>();

        public static readonly IEnumerable<RatingCategory> ratingCategories = Enum.GetValues(typeof(RatingCategory)).Cast<RatingCategory>();

        private static HashSet<int> processedBattles = new HashSet<int>();

        public static bool Initialized { get; private set; }

        private static object processingLock = new object();

        private static Dictionary<int, int> accountAliases = new Dictionary<int, int>();

        public static void Init()
        {
            Initialized = false;
            ratingCategories.ForEach(category => whr[category] = new WholeHistoryRating(category));

            Task.Factory.StartNew(() => {
                lock (processingLock)
                {
                    try
                    {
                        UpdateRatingIds();
                        using (ZkDataContext data = new ZkDataContext())
                        {
                            data.Database.CommandTimeout = 240;
                            for (int month = 10*12; month > 0; month--)
                            {
                                DateTime minStartTime = DateTime.Now.AddMonths(-month);
                                DateTime maxStartTime = DateTime.Now.AddMonths(-month + 1);
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

        public static void UpdateRatingIds()
        {
            using (ZkDataContext db = new ZkDataContext())
            {
                accountAliases = db.Accounts.Where(x => x.WhrAlias > 0).ToDictionary(x => x.AccountID, x => x.WhrAlias);
            }
        }

        public static int GetRatingId(int accountID)
        {
            return accountAliases.ContainsKey(accountID) ? accountAliases[accountID] : accountID;
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

        //Erases old SpringBattle from Rating History
        public static void RemoveResult(SpringBattle battle)
        {
            if (!Initialized) return;
            ProcessBattle(battle, true, true);
        }

        //Processes old SpringBattle with predetermined applicable ratings
        public static void ReprocessResult(SpringBattle battle)
        {
            if (!Initialized) return;
            ProcessBattle(battle, true);
        }

        //Processes new SpringBattle and determines applicable ratings
        public static void ProcessResult(SpringBattle battle, SpringBattleContext result)
        {
            if (!Initialized) return;
            FillApplicableRatings(battle, result);
            ProcessBattle(battle);
        }

        private static void FillApplicableRatings(SpringBattle battle, SpringBattleContext result)
        {
            battle.ApplicableRatings = 0;
            if (battle.HasBots) return;
            if (battle.IsMission) return;
            if (battle.SpringBattlePlayers?.Where(x => !x.IsSpectator).Select(x => x.AllyNumber).Distinct().Count() < 2) return;
            if (battle.ResourceByMapResourceID?.MapIsSpecial == true) return;
            
            //only count balanced custom matches for elo
            if (battle.Mode == AutohostMode.None && battle.SpringBattlePlayers?.Where(x => !x.IsSpectator).GroupBy(x => x.AllyNumber).Select(x => x.Count()).Distinct().Count() > 1) return;
            if (battle.Duration < GlobalConst.MinDurationForElo) return;

            //don't mark battles for ratings if they can't be rated
            ICollection<int> winners = battle.SpringBattlePlayers.Where(p => p.IsInVictoryTeam && !p.IsSpectator).Select(p => RatingSystems.GetRatingId(p.AccountID)).Distinct().ToList();
            ICollection<int> losers = battle.SpringBattlePlayers.Where(p => !p.IsInVictoryTeam && !p.IsSpectator).Select(p => RatingSystems.GetRatingId(p.AccountID)).Distinct().ToList();
            if (winners.Count == 0 || losers.Count == 0 || winners.Intersect(losers).Count() != 0) return;

            battle.ApplicableRatings |= (RatingCategoryFlags)result.LobbyStartContext.ApplicableRating;
            //Optionally add other flags here, like a casual or overall rating
        }

        private static void ProcessBattle(SpringBattle battle, bool reprocessingBattle = false, bool removeBattle = false)
        {
            lock (processingLock)
            {
                int battleID = -1;
                try
                {
                    battleID = battle.SpringBattleID;
                    if (reprocessingBattle)
                    {
                        ratingCategories.ForEach(c => whr[c].ProcessBattle(battle, removeBattle || !IsCategory(battle, c)));
                    }
                    else
                    {
                        if (processedBattles.Contains(battleID)) return;
                        processedBattles.Add(battleID);
                        ratingCategories.Where(c => IsCategory(battle, c)).ForEach(c => whr[c].ProcessBattle(battle));
                        latestBattle = battleID;
                    }
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
                    var rating = RatingCategory.Planetwars;
                    using (var db = new ZkDataContext())
                    {
                        IQueryable<Account> players;
                        if (MiscVar.PlanetWarsMode == PlanetWarsModes.PreGame)
                        {
                            players = db.Accounts.Where(x => x.LastLogin > maxAge && x.FactionID == factionID);
                            rating = RatingCategory.Casual;
                        }
                        else
                        {
                            players = db.Accounts.Where(x => x.PwAttackPoints > 0 && x.FactionID == factionID);
                        }
                        count = players.Count();
                        skill = count > 0 ? (int)Math.Round(players.SelectMany(x => x.AccountRatings).Where(x => x.RatingCategory == rating).Select(x => x.RealElo).Average()) : 1500;
                        factionCache[factionID] = new Tuple<int, int, int>(latestBattle, count, skill);
                    }
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
    
}
