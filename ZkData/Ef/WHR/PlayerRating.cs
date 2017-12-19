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
        public float Percentile;
        public int Rank;
        public float RealElo;
        public float Uncertainty {
            get
            {
                return LastUncertainty + (float)Math.Sqrt((CurrentDate - LastGameDate) * GlobalConst.EloDecayPerDaySquared);
            }
        }
        public float Elo {
            get
            {
                return RealElo - Math.Min(200, Math.Max(0, Uncertainty - 20)) * 2; //dont reduce value for active players
            }
        }

        [JsonProperty]
        public readonly float LastUncertainty;
        [JsonProperty]
        public readonly int LastGameDate;
        [JsonProperty]
        private int CurrentDate;

        public void ApplyLadderUpdate(int Rank, float Percentile, int CurrentDate)
        {
            this.Rank = Rank;
            this.Percentile = Percentile;
            this.CurrentDate = CurrentDate;
        }

        [JsonConstructor]
        public PlayerRating(int Rank, float Percentile, float RealElo, float LastUncertainty, int LastGameDate, int CurrentDate)
        {
            this.Percentile = Percentile;
            this.Rank = Rank;
            this.RealElo = RealElo;
            this.LastUncertainty = LastUncertainty;
            this.LastGameDate = LastGameDate;
            this.CurrentDate = CurrentDate;
        }
    }
}
