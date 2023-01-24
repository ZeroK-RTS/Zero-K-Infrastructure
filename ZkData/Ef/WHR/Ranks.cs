using PlasmaShared;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
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
        public static string[] RankNames = new string[] { "Nebulous", "Brown Dwarf", "Red Dwarf", "Subgiant", "Giant", "Supergiant", "Neutron Star", "Singularity", "Space Lobster" };

        public static bool ValidateRank(int rank)
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

        public static RankProgress GetRankProgress(Account acc, IRatingSystem ratingSystem)
        {

            var rating = ratingSystem.GetPlayerRating(acc.AccountID);
            //if (rating.Rank == int.MaxValue) return null;
            // ignore inactivity
            if (ratingSystem.GetActivePlayers() < GlobalConst.LadderSize) return null;
            // don't count unused rating systems, e.g. planetwars
            var rank = acc.Rank;
            var bracket = ratingSystem.GetPercentileBracket(rank);
            var stdevUp = 0.0;
            var stdevDown = 0.0;
            var bracketOverlap = 0.2;
            //sets overlap in next rank: player needs to be at least this amount within the next rank
            var myBracketWidth = ratingSystem.GetPercentileBracket(rank).UpperEloLimit - ratingSystem.GetPercentileBracket(rank).LowerEloLimit;
            if (ValidateRank(rank + 1)) {
                var nextBracketWidth = ratingSystem.GetPercentileBracket(rank + 1).UpperEloLimit - ratingSystem.GetPercentileBracket(rank + 1).LowerEloLimit;
                stdevUp = Math.Min(myBracketWidth, nextBracketWidth)*bracketOverlap;
            }
            if (ValidateRank(rank - 1)) {
                var prevBracketWidth = ratingSystem.GetPercentileBracket(rank - 1).UpperEloLimit - ratingSystem.GetPercentileBracket(rank - 1).LowerEloLimit;
                stdevDown = Math.Min(myBracketWidth, prevBracketWidth) * bracketOverlap;
            }
            var rankCeil = bracket.UpperEloLimit + stdevUp;
            var rankFloor = bracket.LowerEloLimit - stdevDown;
            //Trace.TraceInformation(acc.Name + ": bracket(" + bracket.LowerEloLimit + ", " + bracket.UpperEloLimit + ") requirements (" + rankFloor + ", " + rankCeil + ") current: " + rating.RealElo + " -> progress: " + bestProgress);
            return new RankProgress()
            {
                ProgressRatio = (float)Math.Min(1, (rating.LadderElo - rankFloor) / (rankCeil - rankFloor)),
                RankCeilElo = (float)rankCeil,
                RankFloorElo = (float)rankFloor,
                CurrentElo = rating.LadderElo
            };
        }

        public static float GetRankProgress(Account acc)
        {
            float bestProgress = 0;
            bool isActive = false;
            foreach (var ratingSystem in RatingSystems.GetRatingSystems())
            {
                var progress = GetRankProgress(acc, ratingSystem);
                if (progress != null) {
                    isActive = true;
                    bestProgress = Math.Max(bestProgress, progress.ProgressRatio);
                }
            }
            if (!isActive) return 0.001f;
            return bestProgress;
        }

        public static float UpdateLadderRating(Account acc, RatingCategory cat, float targetRating, int allyCount, bool allowGain, bool allowLoss, float classicEloChange, ZkDataContext db)
        {
            var rating = acc.AccountRatings.Where(x => x.RatingCategory == cat).FirstOrDefault();
            var ladderElo = rating?.LadderElo ?? WholeHistoryRating.DefaultRating.LadderElo;
            if (!allowLoss && !allowGain)
            {
                Trace.TraceWarning("No rating change allowed for player " + acc.Name + "(" + acc.AccountID + ")");
                return (float)ladderElo;
            }
            if (float.IsNaN(targetRating))
            {
                Trace.TraceWarning("Target rating for player " + acc.Name + "(" + acc.AccountID + ") is NaN");
                return (float)ladderElo;
            }
            var delta = targetRating - ladderElo;
            float classicWeight = 1 - Math.Abs(delta) / (30 + Math.Abs(delta));
            delta *= GlobalConst.LadderEloSmoothingFactor; //smooth out elo changes.
            delta = Math.Min(GlobalConst.LadderEloMaxChange, delta); //clip rating change to allowed limits
            delta = Math.Max(-GlobalConst.LadderEloMaxChange, delta);
            if (!allowGain) delta = Math.Min(-GlobalConst.LadderEloMinChange, delta);
            if (!allowLoss) delta = Math.Max(GlobalConst.LadderEloMinChange, delta);

            double ladderEloBefore = ladderElo;
            delta /= allyCount; // Scale WHR leashing with classic elo rate of change.
            ladderElo += delta * (1 - classicWeight) + classicWeight * classicEloChange;
            if (rating != null)
            {
                rating.UpdateLadderElo(ladderElo);
                db.Entry(rating).State = System.Data.Entity.EntityState.Modified;
            }
            else
            {
                Trace.TraceInformation(string.Format("WHR LadderElo update for player {0} not directly saved to db", acc.Name));
            }
            Trace.TraceInformation(string.Format("WHR LadderElo update for player {0} ({1}) from {2} -> {3}, targeting {4}. WHR Change: {5}, Elo Change: {6}", acc.Name, acc.AccountID, ladderEloBefore, ladderElo, targetRating, delta, classicEloChange));
            return (float)ladderElo;
        }

        public static bool UpdateRank(Account acc, bool allowUprank, bool allowDownrank, ZkDataContext db)
        {
            var progress = GetRankProgress(acc);
            if (progress > 0.99999f && allowUprank && ValidateRank(acc.Rank + 1))
            {
                acc.Rank++;
                Trace.TraceInformation(acc.Name + " has ranked up to " + acc.Rank);
                if (!ValidateRank(acc.Rank))
                {
                    Trace.TraceWarning("Correcting invalid rankup for player " + acc.AccountID + ": " + acc.Rank);
                    acc.Rank = RankBackgroundImages.Length - 1;
                }
                return true;
            } 
            if (progress < 0.00001f && allowDownrank && ValidateRank(acc.Rank - 1))
            {
                acc.Rank--;
                Trace.TraceInformation(acc.Name + " has ranked down to " + acc.Rank);
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

        public class RankProgress
        {
            public float ProgressRatio;
            public float RankFloorElo;
            public float RankCeilElo;
            public float CurrentElo;
        }
    }

    public class RankBracket
    {
        public float UpperEloLimit, LowerEloLimit;
    }
    
}
