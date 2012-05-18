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
        
        [Description("Medium teams (disabled)")]
        MediumTeams = 1,
        
        [Description("PlanetWars (disabled)")]
        Planetwars = 2,
        
        [Description("1v1")]
        Game1v1 = 3,
        
        [Description("FFA (free for all)")]
        GameFFA = 4,
        
        [Description("Cooperative (vs AI)")]
        GameChickens = 5,
        
        [Description("Small teams (2v2-5v5)")]
        SmallTeams = 6,
        
        [Description("Big teams (5v5+)")]
        BigTeams = 7,
    }

}
