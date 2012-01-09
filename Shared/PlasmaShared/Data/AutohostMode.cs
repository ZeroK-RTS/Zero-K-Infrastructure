using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ZkData
{
    public enum AutohostMode
    {
        None = 0,
        [Description("Teams")]
        GameTeams = 1,
        [Description("PlanetWars online campaign")]
        Planetwars = 2,
        [Description("1v1")]
        Game1v1 = 3,
        [Description("FFA (free for all)")]
        GameFFA = 4,
        [Description("Cooperative (vs chickens/CAI)")]
        GameChickens = 5
    }

}
