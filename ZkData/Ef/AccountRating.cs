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
        public RatingCategory ratingCategory;

        public float Percentile;
        public int Rank;
        public float RealElo;


        private static readonly CompiledExpression<AccountRating, float> uncertaintyExpression = DefaultTranslationOf<AccountRating>.Property(e => e.Uncertainty).Is(e => e.LastUncertainty + (float)Math.Sqrt((RatingSystems.ConvertDateToDays(DateTime.Now) - e.LastGameDate) * GlobalConst.EloDecayPerDaySquared));
        private static readonly CompiledExpression<AccountRating, float> eloExpression = DefaultTranslationOf<AccountRating>.Property(e => e.Elo).Is(e => e.RealElo - Math.Min(200, Math.Max(0, e.Uncertainty - 20)) * 2);

        public float Elo => eloExpression.Evaluate(this);
        public float Uncertainty => uncertaintyExpression.Evaluate(this);

        public float LastUncertainty;
        public int LastGameDate;

        public void UpdateFromRatingSystem(PlayerRating rating)
        {
            this.Percentile = rating.Percentile;
            this.Rank = rating.Rank;
            this.RealElo = rating.RealElo;
            this.LastUncertainty = rating.LastUncertainty;
            this.LastGameDate = rating.LastGameDate;
        }
        
        public AccountRating(int AccountID, RatingCategory ratingCategory)
        {
            this.ratingCategory = ratingCategory;
            this.AccountID = AccountID; 
            this.Percentile = 1;
            this.Rank = int.MaxValue;
            this.RealElo = 1500;
            this.LastUncertainty = float.PositiveInfinity;
            this.LastGameDate = 0;
        }
    }
}
