using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZkData;
using System.Data.Entity;
using Newtonsoft.Json;
using Ratings;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Linq.Translations;
using PlasmaShared;

namespace ZkData
{
    public class AccountRating
    {

        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public RatingCategory RatingCategory { get; set; }

        public double Percentile { get; set; }
        [Index]
        public bool IsRanked{ get; set; }
        [Index]
        public double RealElo { get; set; }
        [Index]
        public double Elo { get; set; }
        [Index]
        public double? LadderElo { get; set; }

        public double EloStdev { get; set; }

        
        public PlayerRating ToUnrankedPlayerRating()
        {
            return new PlayerRating(int.MaxValue, (float)Percentile, (float)RealElo, (float)EloStdev, 0, 0, 0, (float)LadderElo, false);
        }
        
        public void UpdateLadderElo(double ladderElo)
        {
            if (double.IsNaN(ladderElo)) Trace.TraceWarning("Tried to set LadderElo for " + AccountID + " to NaN");
            else LadderElo = ladderElo;
        }

        public void UpdateFromRatingSystem(PlayerRating rating)
        {
            if (float.IsNaN(rating.Percentile)) Trace.TraceWarning("Tried to set Percentile for " + AccountID + " to NaN");
            else this.Percentile = rating.Percentile;
            this.IsRanked = rating.Rank < int.MaxValue;
            if (float.IsNaN(rating.RealElo)) Trace.TraceWarning("Tried to set RealElo for " + AccountID + " to NaN");
            else this.RealElo = rating.RealElo;
            if (float.IsNaN(rating.EloStdev)) Trace.TraceWarning("Tried to set EloStdev for " + AccountID + " to NaN");
            else this.EloStdev = rating.EloStdev;
            if (float.IsNaN(rating.Elo)) Trace.TraceWarning("Tried to set Elo for " + AccountID + " to NaN");
            else this.Elo = rating.Elo;
            UpdateLadderElo(rating.LadderElo);
        }
        
        public AccountRating(int AccountID, RatingCategory ratingCategory)
        {
            this.RatingCategory = ratingCategory;
            this.AccountID = AccountID;
            UpdateFromRatingSystem(WholeHistoryRating.DefaultRating);
        }

        public AccountRating()
        {

        }


        [ForeignKey(nameof(AccountID))]
        public virtual Account Account { get; set; }
    }
}
