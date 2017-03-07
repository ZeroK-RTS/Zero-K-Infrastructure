using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZkData;

namespace Ratings
{
    public class RatingSystems
    {
        public static Dictionary<RatingCategory, WholeHistoryRating> whr = new Dictionary<RatingCategory, WholeHistoryRating>();

        static RatingSystems()
        {
            Enum.GetValues(typeof(RatingCategory)).Cast<RatingCategory>().ToList().ForEach(category => whr[category] = new WholeHistoryRating());
        }

        public static IRatingSystem GetRatingSystem(RatingCategory category)
        {
            return whr[category];
        }

        public static void ProcessResult(SpringBattle battle)
        {
            whr.Values.ToList().ForEach(r => r.ProcessBattle(battle));
        }
    }

    public enum RatingCategory
    {
        Casual, MatchMaking, Planetwars
    }
}
