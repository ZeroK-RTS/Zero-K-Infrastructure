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
        ///     Possible attack options. Keyed by (AttackerFactionID, PlanetID) — each faction has its own set of
        ///     options with its own attacker and defender pools.
        /// </summary>
        public List<PlanetWarsMatchMaker.AttackOption> AttackOptions { get; set; }

        public PwPhase Phase { get; set; }
        public DateTime PhaseStartTime { get; set; }

        /// <summary>
        ///     Formed attack squads after squad formation runs. Each is an AttackOption with Attackers filled.
        ///     In parallel-turn mode each squad carries its own AttackerFactionID and independent defender pool.
        /// </summary>
        public List<PlanetWarsMatchMaker.AttackOption> FormedSquads { get; set; } = new List<PlanetWarsMatchMaker.AttackOption>();

        public Dictionary<int, PlanetWarsMatchMaker.AttackOption> RunningBattles { get; set; } = new Dictionary<int, PlanetWarsMatchMaker.AttackOption>();
        public PlanetWarsMatchMakerState() { }
    }
}
