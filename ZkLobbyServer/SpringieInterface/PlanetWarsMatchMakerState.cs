using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroKWeb
{
    public enum PwPhase
    {
        AttackCollect = 0,
        DefendCollect = 1
    }

    public class PlanetWarsMatchMakerState
    {
        /// <summary>
        ///     Possible attack options / planets to vote on
        /// </summary>
        public List<PlanetWarsMatchMaker.AttackOption> AttackOptions { get; set; }
        public DateTime AttackerSideChangeTime { get; set; }
        public int AttackerSideCounter { get; set; }

        public PwPhase Phase { get; set; }
        public DateTime PhaseStartTime { get; set; }

        /// <summary>
        ///     Formed attack squads after squad formation runs. Each is an AttackOption with Attackers filled.
        /// </summary>
        public List<PlanetWarsMatchMaker.AttackOption> FormedSquads { get; set; } = new List<PlanetWarsMatchMaker.AttackOption>();

        /// <summary>
        ///     Defender volunteers per planet during DefendCollect phase. Key = PlanetID, Value = list of player names.
        /// </summary>
        public Dictionary<int, List<string>> DefenderVotes { get; set; } = new Dictionary<int, List<string>>();

        public Dictionary<int, PlanetWarsMatchMaker.AttackOption> RunningBattles { get; set; } = new Dictionary<int, PlanetWarsMatchMaker.AttackOption>();
        public PlanetWarsMatchMakerState() { }
    }
}
