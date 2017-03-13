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
        public float Elo { get; private set; }
        public float Uncertainty { get
            {
                return UncertaintyFunc.Invoke();
            }
        }

        [JsonProperty]
        private readonly Func<float> UncertaintyFunc;

        public PlayerRating(float Elo, float Uncertainty) : this(Elo, () => Uncertainty)
        {
        }

        [JsonConstructor]
        public PlayerRating(float Elo, Func<float> UncertaintyCalculator)
        {
            this.Elo = Elo;
            this.UncertaintyFunc = UncertaintyCalculator;
        }
    }
}
