using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer.autohost
{
    public abstract class ServerBattleCommand
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
        public abstract BattleCommandAccess Access { get; }

        /// <summary>
        ///     Creates instance
        /// </summary>
        /// <returns></returns>
        public abstract ServerBattleCommand Create();

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

        /// <summary>
        /// Determines command permissions
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public virtual CommandExecutionRight RunPermissions(ServerBattle battle, string userName)
        {
            if (Access == BattleCommandAccess.NoCheck) return CommandExecutionRight.Run;
            
            UserBattleStatus user = null;
            if (userName != null) battle.Users.TryGetValue(userName, out user);
            var hasAdminRights = userName == battle.FounderName || user?.LobbyUser?.IsAdmin == true;

            var s = battle.spring;
            bool isSpectator = true;
            int count = 0;
            if (s.IsRunning)
            {
                if (s.StartContext.Players.Any(x => x.Name == userName && !x.IsSpectator)) isSpectator = false;
                count = s.StartContext.Players.Count(x=>!x.IsSpectator);
            }
            else
            {
                if (user?.IsSpectator == false) isSpectator = false;
                count = battle.Users.Count(x => !x.Value.IsSpectator);
            }
            return CommandExecutionRight.Vote;

            var defPerm = hasAdminRights ? CommandExecutionRight.Run : (isSpectator ? CommandExecutionRight.None : CommandExecutionRight.Vote);
            if (defPerm == CommandExecutionRight.None) return defPerm;
            if (defPerm == CommandExecutionRight.Vote && count<=1) defPerm = CommandExecutionRight.Run;

                if (Access == BattleCommandAccess.Anywhere) return defPerm;
            if (Access == BattleCommandAccess.Ingame && s.IsRunning) return defPerm;
            if (Access == BattleCommandAccess.NotIngame && !s.IsRunning) return defPerm;
            if (Access == BattleCommandAccess.IngameVote && s.IsRunning) return CommandExecutionRight.Vote;

            return CommandExecutionRight.None;
        }
    }
}