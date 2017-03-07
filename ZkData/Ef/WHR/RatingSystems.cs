using System;
using System.Collections.Generic;
using System.Linq;
using ZkData;

namespace Ratings
{
    public class RatingSystems
    {
        public static Dictionary<RatingCategory, WholeHistoryRating> whr = new Dictionary<RatingCategory, WholeHistoryRating>();

        public static readonly List<RatingCategory> ratingCategories = Enum.GetValues(typeof(RatingCategory)).Cast<RatingCategory>().ToList();

        private static HashSet<SpringBattle> processedBattles = new HashSet<SpringBattle>();

        static RatingSystems()
        {
            ratingCategories.ForEach(category => whr[category] = new WholeHistoryRating());

            ZkDataContext data = new ZkDataContext();
            foreach (SpringBattle b in data.SpringBattles.AsNoTracking()) ProcessResult(b);
        }

        public static IRatingSystem GetRatingSystem(RatingCategory category)
        {
            return whr[category];
        }

        public static void ProcessResult(SpringBattle battle)
        {
            if (processedBattles.Contains(battle)) return;
            processedBattles.Add(battle);
            ratingCategories.Where(c => IsCategory(battle, c)).ToList().ForEach(c => whr[c].ProcessBattle(battle));
        }

        private static bool IsCategory(SpringBattle battle, RatingCategory category)
        {
            switch (category)
            {
                case RatingCategory.Casual:
                    return false && !(battle.IsMission || battle.HasBots || (battle.PlayerCount < 2) || (battle.ResourceByMapResourceID.MapIsSpecial == true));
                case RatingCategory.MatchMaking:
                    return battle.IsMatchMaker;
                case RatingCategory.Planetwars:
                    return false; //how?
            }
            return false;
        }
    }

    public enum RatingCategory
    {
        Casual, MatchMaking, Planetwars
    }
}
