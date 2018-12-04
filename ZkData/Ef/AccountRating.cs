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
        public int Rank { get; set; }
        [Index]
        public double RealElo { get; set; }
        [Index]
        public double Elo { get; set; }
        
        public double Uncertainty { get; set; }
        
        public PlayerRating ToPlayerRating()
        {
            return new PlayerRating(Rank, (float)Percentile, (float)RealElo, (float)Uncertainty, 0, 0, 0);
        }

        public void UpdateFromRatingSystem(PlayerRating rating)
        {
            this.Percentile = rating.Percentile;
            this.Rank = rating.Rank;
            this.RealElo = rating.RealElo;
            this.Uncertainty = rating.Uncertainty;
            this.Elo = rating.Elo;
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
