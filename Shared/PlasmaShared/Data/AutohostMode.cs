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
       
        [Description("PlanetWars")]
        Planetwars = 2,
        
        [Description("1v1")]
        Game1v1 = 3,
        
        [Description("FFA (free for all)")]
        GameFFA = 4,
        
        [Description("Cooperative (vs AI)")]
        GameChickens = 5,
        
        [Description("Teams")]
        Teams = 6,
        
        [Description("Small teams")]
        SmallTeams = 7,

        [Description("Newbies only")]
        LowSkill = 8,

        [Description("Experienced only")]
        HighSkill = 9,
    }

}
