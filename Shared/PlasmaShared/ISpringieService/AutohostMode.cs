using System.ComponentModel;

namespace PlasmaShared
{
    public enum AutohostMode
    {
        [Description("Cooperative")]
        GameChickens = 5,

        [Description("Teams")]
        Teams = 6,

        [Description("1v1")]
        Game1v1 = 3,

        [Description("FFA")]
        GameFFA = 4,

        [Description("Custom")]
        None = 0,

        [Description("PlanetWars")]
        Planetwars = 2,
    }

}
