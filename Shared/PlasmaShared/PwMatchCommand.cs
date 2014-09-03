using System.Collections.Generic;
using ZkData;

namespace PlasmaShared
{
    /// <summary>
    ///     Command sent to lobbies (with pw options)
    /// </summary>
    public class PwMatchCommand
    {
        public enum ModeType
        {
            Clear = 0,
            Attack = 1,
            Defend = 2
        }

        public ModeType Mode;

        public List<VoteOption> Options = new List<VoteOption>();

        public PwMatchCommand(ModeType mode)
        {
            Mode = mode;
        }

        public class VoteOption
        {
            public int Count;
            public string Map;
            public int Needed = GlobalConst.PlanetWarsMatchSize;
            public int PlanetID;
            public string PlanetName;
        }
    }
}