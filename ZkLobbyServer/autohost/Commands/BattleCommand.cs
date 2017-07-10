using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public abstract class BattleCommand
    {
        /// <summary>
        ///     Help line displayed
        /// </summary>
        public abstract string Help { get; }

        /// <summary>
        ///     Command shortcut to be used for !shortcut
        /// </summary>
        public abstract string Shortcut { get; }

        /// <summary>
        ///     Access level for the command
        /// </summary>
        public abstract AccessType Access { get; }

        /// <summary>
        ///     Creates instance
        /// </summary>
        /// <returns></returns>
        public abstract BattleCommand Create();

        /// <summary>
        ///     Prepares the command
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="e"></param>
        /// <param name="arguments"></param>
        /// <returns>poll question, null to abort command</returns>
        public abstract string Arm(ServerBattle battle, Say e, string arguments = null);

        /// <summary>
        ///     Execute previously armed command, state stored in class
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="e"></param>
        public abstract Task ExecuteArmed(ServerBattle battle, Say e = null);


        /// <summary>
        ///     Arm and execute in one pass
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="e"></param>
        /// <param name="arguments"></param>
        public async Task Run(ServerBattle battle, Say e, string arguments = null)
        {
            if (Arm(battle, e, arguments) != null) await ExecuteArmed(battle, e);
        }

        public bool IsSpectator(ServerBattle battle, string userName, UserBattleStatus user)
        {
            if (user == null)
            {
                if (userName != null) battle.Users.TryGetValue(userName, out user);
            }
            bool isSpectator = true;
            var s = battle.spring;
            if (s.IsRunning)
            {
                if (s.LobbyStartContext.Players.Any(x => x.Name == userName && !x.IsSpectator)) isSpectator = false;
            }
            else
            {
                if (user?.IsSpectator == false) isSpectator = false;
            }
            return isSpectator;
        }

        /// <summary>
        /// Determines command permissions
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public virtual RunPermission GetRunPermissions(ServerBattle battle, string userName)
        {
            if (Access == AccessType.NoCheck) return RunPermission.Run;
            
            UserBattleStatus user = null;
            if (userName != null) battle.Users.TryGetValue(userName, out user);
            var hasAdminRights = userName == battle.FounderName || user?.LobbyUser?.IsAdmin == true;

            var s = battle.spring;
            bool isSpectator = IsSpectator(battle, userName, user);
            bool isAway = user?.LobbyUser?.IsAway == true;
            int count = 0;
            if (s.IsRunning)
            {
                count = s.LobbyStartContext.Players.Count(x=>!x.IsSpectator);
            }
            else
            {
                count = battle.Users.Count(x => !x.Value.IsSpectator);
            }

            var defPerm = hasAdminRights ? RunPermission.Run : (isSpectator || isAway ? RunPermission.None : RunPermission.Vote);
            if (defPerm == RunPermission.None) return defPerm;
            if (defPerm == RunPermission.Vote && count<=1) defPerm = RunPermission.Run;

            if (Access == AccessType.Anywhere) return defPerm;
            if (Access == AccessType.Ingame && s.IsRunning) return defPerm;
            if (Access == AccessType.NotIngame && !s.IsRunning) return defPerm;
            if (Access == AccessType.IngameVote && s.IsRunning) return RunPermission.Vote;

            return RunPermission.None;
        }

        public enum AccessType
        {
            [Description("At any time, by anyone, no vote needed")]
            NoCheck = 0,

            /// <summary>
            /// Can be executed ingame/offgame by non-spectators (vote) or admins or founder (direct)
            /// </summary>
            [Description("At any time, by players, might need a vote")]
            Anywhere = 1,

            /// <summary>
            /// Can be executed ingame by non-spectators (vote) or admins or founder (direct)
            /// </summary>
            [Description("When game running, by players, might need a vote")]
            Ingame = 2,

            /// <summary>
            /// Can be executed not-ingame by non-spectators (vote) or admins or founder (direct)
            /// </summary>
            [Description("When game not running, by players, might need a vote")]
            NotIngame = 3,

            /// <summary>
            /// Can be executed ingame only as a vote by non-spectators or admins or founder
            /// </summary>
            [Description("When game running, by players, needs vote")]
            IngameVote = 4,
        }


        public enum RunPermission
        {
            None = 0,
            Vote = 1,
            Run = 2
        }


    }
}