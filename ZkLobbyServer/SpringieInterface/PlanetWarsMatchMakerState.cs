using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroKWeb
{
    public class PlanetWarsMatchMakerState
    {
        /// <summary>
        ///     Possible attack options
        /// </summary>
        public List<PlanetWarsMatchMaker.AttackOption> AttackOptions { get; set; }
        public DateTime AttackerSideChangeTime { get; set; }
        public int AttackerSideCounter { get; set; }
        public PlanetWarsMatchMaker.AttackOption Challenge { get; set; }

        public DateTime? ChallengeTime { get; set; }

        public Dictionary<int, PlanetWarsMatchMaker.AttackOption> RunningBattles { get; set; }
        public PlanetWarsMatchMakerState() { }
    }
}