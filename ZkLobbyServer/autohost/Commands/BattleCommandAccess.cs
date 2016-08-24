using System.Collections.Generic;
using System.Linq;

namespace ZkLobbyServer.autohost
{
    public enum BattleCommandAccess
    {
        NoCheck = 0,

        /// <summary>
        /// Can be executed ingame/offgame by non-spectators (vote) or admins or founder (direct)
        /// </summary>
        Anywhere = 1,

        /// <summary>
        /// Can be executed ingame by non-spectators (vote) or admins or founder (direct)
        /// </summary>
        Ingame = 2,

        /// <summary>
        /// Can be executed not-ingame by non-spectators (vote) or admins or founder (direct)
        /// </summary>
        NotIngame = 3,

        /// <summary>
        /// Can be executed ingame only as a vote by non-spectators or admins or founder
        /// </summary>
        IngameVote = 4,
    }
}