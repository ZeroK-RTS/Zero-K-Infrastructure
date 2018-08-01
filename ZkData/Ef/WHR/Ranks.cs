using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ZkData;

namespace Ratings
{
    public enum RankSelector
    {
        [Display(Name="Nebulous")]
        Grey = 0,
        [Display(Name="Brown Dwarf")]
        Brown = 1,
        [Display(Name="Red Dwarf")]
        Red = 2,
        [Display(Name="Subgiant")]
        Orange = 3,
        [Display(Name="Giant")]
        Yellow = 4,
        [Display(Name="Supergiant")]
        White = 5,
        [Display(Name="Neutron Star")]
        Blue = 6,
        [Display(Name="Singularity")]
        Purple = 7,
        [Display(Name="Any")]
        Undefined = 8,
    }

    public class Ranks
    {
        public static readonly float[] Percentiles = {float.MaxValue, 0.8f, 0.6f, 0.4f, 0.2f, 0.1f, 0.05f, 0.01f};

        
        public static string[] RankBackgroundImages = new string[] { "infrared", "brown", "red", "orange", "yellow", "blue", "neutron", "black" };
        public static string[] RankNames = new string[] { "Nebulous", "Brown Dwarf", "Red Dwarf", "Subgiant", "Giant", "Supergiant", "Neutron Star", "Singularity", "Sadde" };

        private static bool ValidateRank(int rank)
        {
            return rank >= 0 && rank < RankBackgroundImages.Length;
        }

        public static string GetRankBackgroundImagePath(Account acc)
        {
            int rank = acc.Rank;
            if (!ValidateRank(rank))
            {
                Trace.TraceWarning("Invalid rank for player " + acc.AccountID + ": " + rank);
                rank = 0;
            }
            return string.Format("/img/rankbg/{0}.png", RankBackgroundImages[rank]);
        }

        public static float GetRankProgress(Account acc)
        {
            float bestProgress = 0;
            bool isActive = false;
            foreach (var ratingSystem in RatingSystems.GetRatingSystems())
            {
                if (ratingSystem.GetActivePlayers() < 50) continue;
                var rating = ratingSystem.GetPlayerRating(acc.AccountID);
                if (rating.Rank == int.MaxValue) continue;
                isActive = true;
                var stdev = Math.Min(10000, rating.Uncertainty);
                var bracket = ratingSystem.GetPercentileBracket(acc.Rank);
                var rankCeil = bracket.UpperEloLimit + stdev;
                var rankFloor = bracket.LowerEloLimit - stdev;
                bestProgress = Math.Max(bestProgress, Math.Min(1, (rating.RealElo - rankFloor) / (rankCeil - rankFloor)));
                //Trace.TraceInformation(acc.Name + ": bracket(" + bracket.LowerEloLimit + ", " + bracket.UpperEloLimit + ") requirements (" + rankFloor + ", " + rankCeil + ") current: " + rating.RealElo + " -> progress: " + bestProgress);
            }
            if (!isActive) return 0.001f;
            return bestProgress;
        }

        public static bool UpdateRank(Account acc, bool allowUprank, bool allowDownrank, ZkDataContext db)
        {
            var progress = GetRankProgress(acc);
            if (progress > 0.99999f && allowUprank)
            {
                acc.Rank++;
                if (!ValidateRank(acc.Rank))
                {
                    Trace.TraceWarning("Correcting invalid rankup for player " + acc.AccountID + ": " + acc.Rank);
                    acc.Rank = RankBackgroundImages.Length - 1;
                }
                return true;
            } 
            if (progress < 0.00001f && allowDownrank)
            {
                acc.Rank--;
                if (!ValidateRank(acc.Rank))
                {
                    Trace.TraceWarning("Correcting invalid rankdown for player " + acc.AccountID + ": " + acc.Rank);
                    acc.Rank = 0;
                }
                return true;
            }
            if (!ValidateRank(acc.Rank))
            {
                Trace.TraceWarning("Correcting invalid rank for player " + acc.AccountID + ": " + acc.Rank);
                acc.Rank = 0;
                return true;
            }
            return false;
        }
    }

    public class RankBracket
    {
        public float UpperEloLimit, LowerEloLimit;
    }
    
}
