using System;
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

        public ModeType Mode { get; set; }


        public DateTime Deadline { get; set; }

        public List<VoteOption> Options { get; set; }

        public PwMatchCommand(ModeType mode)
        {
            Mode = mode;
            Options = new List<VoteOption>();
        }

        public class VoteOption
        {
            public int Count { get; set; }
            public string Map { get; set; }
            public int Needed { get; set; }
            public int PlanetID { get; set; }
            public string PlanetName { get; set; }

            public VoteOption()
            {
                Needed = GlobalConst.PlanetWarsMatchSize;
            }
        }
    }
}