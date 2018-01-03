using System;
using ZkData;

namespace Ratings
{
    public class Ranks
    {
        public static readonly float[] Percentiles = { 0.01f, 0.05f, 0.1f, 0.2f, 0.4f, 0.6f, 0.8f, float.MaxValue};

        public static float[] Brackets = { float.MinValue, 1200f, 1400f, 1600f, 1800f, 2000f, 2200f, 2400f, float.MaxValue};
        
        public static string[] RankBackgroundImages = new string[] { "infrared", "brown", "red", "orange", "yellow", "blue", "neutron", "black" };
        public static string[] RankNames = new string[] { "Nebulous", "Brown Dwarf", "Red Dwarf", "Subgiant", "Giant", "Supergiant", "Neutron Star", "Singularity", "Space Lobster" };

        public static string GetRankBackgroundImagePath(Account acc)
        {
            return string.Format("/img/rankbg/{0}.png", RankBackgroundImages[acc.Rank]);
        }

        public static float GetRankProgress(Account acc)
        {
            var rating = acc.GetBestRating();
            var rankCeil = Brackets[acc.Rank + 1] + rating.Uncertainty;
            var rankFloor = Brackets[acc.Rank] - rating.Uncertainty;
            return Math.Min(1, Math.Max(0, (rating.RealElo - rankFloor) / (rankCeil - rankFloor)));
        }

        public static void UpdateRank(Account acc, bool allowUprank, bool allowDownrank, ZkDataContext db)
        {
            var rating = acc.GetBestRating();
            var rankCeil = Brackets[acc.Rank + 1] + rating.Uncertainty;
            var rankFloor = Brackets[acc.Rank] - rating.Uncertainty;
            if (rating.RealElo > rankCeil && allowUprank)
            {
                acc.Rank++;
            } 
            if (rating.RealElo < rankFloor && allowDownrank)
            {
                acc.Rank--;
            }
        }
    }
    
}
