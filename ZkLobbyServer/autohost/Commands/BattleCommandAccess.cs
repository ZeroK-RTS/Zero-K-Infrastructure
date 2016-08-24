using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ZkLobbyServer.autohost
{
    public enum BattleCommandAccess
    {
        [Description("At any time, by anyone, no vote needed")]
        NoCheck = 0,

        /// <summary>
        /// Can be executed ingame/offgame by non-spectators (vote) or admins or founder (direct)
        /// </summary>
        [Description("At any time, by non-spectators, non-owners need a vote")]
        Anywhere = 1,

        /// <summary>
        /// Can be executed ingame by non-spectators (vote) or admins or founder (direct)
        /// </summary>
        [Description("When game running, by non-spectators, non-owners need a vote")]
        Ingame = 2,

        /// <summary>
        /// Can be executed not-ingame by non-spectators (vote) or admins or founder (direct)
        /// </summary>
        [Description("When game not running, by non-spectators, non-owners need a vote")]
        NotIngame = 3,

        /// <summary>
        /// Can be executed ingame only as a vote by non-spectators or admins or founder
        /// </summary>
        [Description("When game running, by non-spectators, needs vote")]
        IngameVote = 4,
    }
}