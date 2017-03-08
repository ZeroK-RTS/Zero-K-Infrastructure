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

        public static readonly bool DISABLE_RATING_SYSTEMS = GlobalConst.Mode == ModeType.Live;

        private static HashSet<int> processedBattles = new HashSet<int>();

        public static bool Initialized { get; private set; }

        private static object processingLock = new object();

        static RatingSystems()
        {
            if (DISABLE_RATING_SYSTEMS) return;
            Initialized = false;
            ratingCategories.ForEach(category => whr[category] = new WholeHistoryRating());

            Task.Factory.StartNew(() => {
                lock (processingLock)
                {
                    ZkDataContext data = new ZkDataContext();
                    foreach (SpringBattle b in data.SpringBattles
                            .Include(x => x.ResourceByMapResourceID)
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
            if (DISABLE_RATING_SYSTEMS) return null;
            return whr[category];
        }

        public static void ProcessResult(SpringBattle battle)
        {
            if (DISABLE_RATING_SYSTEMS) return;
            lock (processingLock)
            {
                int battleID = -1;
                try
                {
                    battleID = battle.SpringBattleID;
                    if (processedBattles.Contains(battleID)) return;
                    processedBattles.Add(battleID);
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
