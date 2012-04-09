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
        [Description("Medium teams (4v4-8v8)")]
        MediumTeams = 1,
        [Description("PlanetWars")]
        Planetwars = 2,
        [Description("1v1")]
        Game1v1 = 3,
        [Description("FFA (free for all)")]
        GameFFA = 4,
        [Description("Cooperative (vs AI)")]
        GameChickens = 5,
        [Description("Small teams (2v2-4v4)")]
        SmallTeams = 6,
        [Description("Big teams (6v6+)")]
        BigTeams = 7,
    }

}
