using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZkData;
using System.Data.Entity;
using Newtonsoft.Json;

namespace Ratings
{
    [Serializable]
    public class PlayerRating
    {
        public readonly double Percentile;
        public readonly int Rank;
        public readonly double RealElo;
        public double Uncertainty {
            get
            {
                return LastUncertainty + (double)Math.Sqrt((RatingSystems.ConvertDateToDays(DateTime.Now) - LastGameDate) * GlobalConst.EloDecayPerDaySquared);
            }
        }
        public double Elo {
            get
            {
                return RealElo - Math.Min(200, Math.Max(0, Uncertainty - 20)) * 2; //dont reduce value for active players
            }
        }

        [JsonProperty]
        public readonly double LastUncertainty;
        [JsonProperty]
        public readonly int LastGameDate;

        public PlayerRating(int Rank, double Percentile, double Elo, double Uncertainty) : this(Rank, Percentile, Elo, Uncertainty, RatingSystems.ConvertDateToDays(DateTime.Now))
        {
        }

        [JsonConstructor]
        public PlayerRating(int Rank, double Percentile, double Elo, double LastUncertainty, int LastGameDate)
        {
            this.Percentile = Percentile;
            this.Rank = Rank;
            this.RealElo = Elo;
            this.LastUncertainty = LastUncertainty;
            this.LastGameDate = LastGameDate;
        }
    }
}
