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
        public RatingCategory RatingCategory { get; set; }

        public double Percentile { get; set; }
        [Index]
        public int Rank { get; set; }
        [Index]
        public double RealElo { get; set; }


        private static readonly CompiledExpression<AccountRating, double> uncertaintyExpression = DefaultTranslationOf<AccountRating>.Property(e => e.Uncertainty).Is(e => e.LastUncertainty + (double)Math.Sqrt((RatingSystems.ConvertDateToDays(DateTime.Now) - e.LastGameDate) * GlobalConst.EloDecayPerDaySquared));
        private static readonly CompiledExpression<AccountRating, double> eloExpression = DefaultTranslationOf<AccountRating>.Property(e => e.Elo).Is(e => e.RealElo - Math.Min(200, Math.Max(0, e.Uncertainty - 20)) * 2);

        public double Elo => eloExpression.Evaluate(this);
        public double Uncertainty => uncertaintyExpression.Evaluate(this);

        public double LastUncertainty { get; set; }
        public int LastGameDate { get; set; }

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
            this.RatingCategory = ratingCategory;
            this.AccountID = AccountID; 
            this.Percentile = 1;
            this.Rank = int.MaxValue;
            this.RealElo = 1500;
            this.LastUncertainty = double.PositiveInfinity;
            this.LastGameDate = 0;
        }
        public AccountRating()
        {

        }


        [ForeignKey(nameof(AccountID))]
        public virtual Account Account { get; set; }
    }
}
