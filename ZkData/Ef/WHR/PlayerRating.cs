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
        public readonly float Percentile;
        public readonly int Rank;
        public readonly float RealElo;
        public float Uncertainty {
            get
            {
                return UncertaintyFunc != null ? UncertaintyFunc.Invoke() : float.PositiveInfinity;
            }
        }
        public float Elo {
            get
            {
                return RealElo - Math.Min(200, Math.Max(0, Uncertainty - 20)) * 2; //dont reduce value for active players
            }
        }

        [JsonProperty]
        private readonly Func<float> UncertaintyFunc;
        [JsonProperty]
        private readonly DateTime lastGame;

        public PlayerRating(int Rank, float Percentile, float Elo, float Uncertainty) : this(Rank, Percentile, Elo, () => Uncertainty)
        {
        }

        [JsonConstructor]
        public PlayerRating(int Rank, float Percentile , float Elo, Func<float> UncertaintyCalculator)
        {
            this.Percentile = Percentile;
            this.Rank = Rank;
            this.RealElo = Elo;
            this.UncertaintyFunc = UncertaintyCalculator;
        }
    }
}
