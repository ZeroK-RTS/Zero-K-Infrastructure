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

        public static readonly List<RatingCategory> ratingCategories = Enum.GetValues(typeof(RatingCategory)).Cast<RatingCategory>().ToList();

        private static HashSet<SpringBattle> processedBattles = new HashSet<SpringBattle>();

        public static bool Initialized { get; private set; }

        private static object processingLock = new object();

        static RatingSystems()
        {
            Initialized = false;
            ratingCategories.ForEach(category => whr[category] = new WholeHistoryRating());

            Task.Factory.StartNew(() => {
                lock (processingLock)
                {
                    ZkDataContext data = new ZkDataContext();
                    foreach (SpringBattle b in data.SpringBattles.Include(x => x.SpringBattleID)
                            .Include(x => x.StartTime)
                            .Include(x => x.ResourceByMapResourceID)
                            .Include(x => x.PlayerCount)
                            .Include(x => x.IsMatchMaker)
                            .Include(x => x.Mode)
                            .Include(x => x.IsMission)
                            .Include(x => x.HasBots)
                            .Include(x => x.SpringBattlePlayers)
                            .Include(x => x.SpringBattleBots)
                            .AsNoTracking()
                            .OrderBy(x => x.SpringBattleID))
                    {
                        ProcessResult(b);
                    }
                    Initialized = true;
                }
            });
        }

        public static IRatingSystem GetRatingSystem(RatingCategory category)
        {
            return whr[category];
        }

        public static void ProcessResult(SpringBattle battle)
        {
            lock (processingLock)
            {
                int battleID = -1;
                try
                {
                    battleID = battle.SpringBattleID;
                    if (processedBattles.Contains(battle)) return;
                    processedBattles.Add(battle);
                    ratingCategories.Where(c => IsCategory(battle, c)).ForEach(c => whr[c].ProcessBattle(battle));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("WHR: Error processing battle (B" + battleID + ")" + ex);
                }
            }
        }

        private static bool IsCategory(SpringBattle battle, RatingCategory category)
        {
            int battleID = -1;
            try
            {
                battleID = battle.SpringBattleID;
                switch (category)
                {
                    case RatingCategory.Casual:
                        return !(battle.IsMission || battle.HasBots || (battle.PlayerCount < 2) || (battle.ResourceByMapResourceID?.MapIsSpecial == true));
                    case RatingCategory.MatchMaking:
                        return battle.IsMatchMaker;
                    case RatingCategory.Planetwars:
                        return battle.Mode == PlasmaShared.AutohostMode.Planetwars; //how?
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
        Casual, MatchMaking, Planetwars
    }
}
