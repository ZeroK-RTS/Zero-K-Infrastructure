using System.ComponentModel;

namespace ZkData
{
    /// <summary>
    /// Whether PlanetWars is running, and in what form. Pure enum, in its own file so it
    /// can be linked into projects that cannot compile GlobalConst.cs - which holds an
    /// IContentServiceClient factory and therefore needs WCF. See
    /// ZkData/EFCORE-MIGRATION.md.
    /// </summary>
    public enum PlanetWarsModes
    {
        [Description("offline")]
        AllOffline = 0,
        [Description("pre-game")]
        PreGame = 1,
        [Description("running")]
        Running = 2
    }
}
