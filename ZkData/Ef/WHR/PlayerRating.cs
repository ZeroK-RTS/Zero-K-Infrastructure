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
        public float LadderElo;
        public float EloStdev {
            get
            {
                return (float)Math.Sqrt((LastNaturalRatingVar + (CurrentDate - LastGameDate) * NaturalRatingVariancePerDay) / GlobalConst.EloToNaturalRatingMultiplierSquared);
            }
        }
        public float Elo {
            get
            {
                return RealElo; //todo: implement simplified derating
            }
        }
        public bool Ranked;

        [JsonProperty]
        public readonly float LastNaturalRatingVar;
        [JsonProperty]
        public readonly float NaturalRatingVariancePerDay;
        [JsonProperty]
        public readonly int LastGameDate;
        [JsonProperty]
        private int CurrentDate;

        public void ApplyLadderUpdate(int Rank, float Percentile, int CurrentDate, bool Ranked)
        {
            this.Rank = Rank;
            this.Percentile = Percentile;
            this.CurrentDate = CurrentDate;
            this.Ranked = Ranked;
        }

        [JsonConstructor]
        public PlayerRating(int Rank, float Percentile, float RealElo, float LastNaturalRatingVar, float LastW2, int LastGameDate, int CurrentDate, float LadderElo, bool Ranked)
        {
            this.Percentile = Percentile;
            this.Rank = Rank;
            this.RealElo = RealElo;
            this.LastNaturalRatingVar = LastNaturalRatingVar;
            this.LastGameDate = LastGameDate;
            this.CurrentDate = CurrentDate;
            this.NaturalRatingVariancePerDay = LastW2;
            this.LadderElo = LadderElo;
            this.Ranked = Ranked;
        }
    }
}
