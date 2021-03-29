using System;
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

        public Func<string, string> GetIneligibilityReasonFunc(ServerBattle battle)
        {
            return x =>
            {
                string reason;
                if (GetRunPermissions(battle, x, out reason) >= BattleCommand.RunPermission.Vote && !IsSpectator(battle, x, battle.Users[x])) return null;
                return reason;
            };
        }

        public bool IsAdmin(ServerBattle battle, string userName)
        {
            UserBattleStatus ubs = null;
            battle.Users.TryGetValue(userName, out ubs);
            if (ubs != null)
                return ubs.LobbyUser.IsAdmin;

            ConnectedUser con = null;
            battle.server.ConnectedUsers.TryGetValue(userName, out con);
            return con?.User?.IsAdmin ?? false;
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
        /// Determines the required margin for a majority vote to pass
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="numVoters"></param>
        /// <returns>number of extra votes to pass, defaults to 1</returns>
        public virtual int GetPollWinMargin(ServerBattle battle, int numVoters)
        {
            return 1;
        }

        /// <summary>
        /// Determines command permissions
        /// </summary>
        /// <param name="battle"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public virtual RunPermission GetRunPermissions(ServerBattle battle, string userName, out string reason)
        {
            reason = "You can't use this command";
            if (Access == AccessType.NoCheck) return RunPermission.Run;
            
            User user = null;
            UserBattleStatus ubs = null;
            if (userName != null)
            {

                battle.Users.TryGetValue(userName, out ubs);
                if (ubs != null)
                {
                    user = ubs.LobbyUser;
                }
                else
                {
                    ConnectedUser con;
                    battle.server.ConnectedUsers.TryGetValue(userName, out con);
                    user = con?.User;
                }
            }

            var hasAdminRights = user?.IsAdmin == true;
            var hasElevatedRights = hasAdminRights || userName == battle.FounderName;

            var s = battle.spring;
            bool isSpectator = IsSpectator(battle, userName, ubs);
            bool isAway = user?.IsAway == true;
            int count = 0;
            if (s.IsRunning)
            {
                count = s.LobbyStartContext.Players.Count(x=>!x.IsSpectator);
            }
            else
            {
                count = battle.Users.Count(x => !x.Value.IsSpectator);
            }

            if (Access == AccessType.Admin && hasAdminRights)
            {
                return RunPermission.Run;
            }
            else if (Access == AccessType.Admin)
            {
                reason = "This command can only be used by moderators.";
                return RunPermission.None;
            }

            var defPerm = hasElevatedRights ? RunPermission.Run : (isSpectator || isAway || user?.BanVotes == true ? RunPermission.None : RunPermission.Vote);

            if (defPerm == RunPermission.None)
            {
                reason = "This command can't be executed by spectators. Join the game to use this command.";
                if (isAway) reason = "You can't vote while being AFK.";
                if (user?.BanVotes == true) reason = "You have been banned from using votes. Check your user page for details.";
                return RunPermission.None;
            }
            if (defPerm == RunPermission.Vote && count<=1) defPerm = RunPermission.Run;
            
            if (Access == AccessType.Anywhere) return defPerm;

            if ((Access == AccessType.NotIngameNotAutohost || Access == AccessType.IngameNotAutohost) && battle.IsAutohost && !hasAdminRights)
            {
                reason = "This command cannot be used on autohosts, either ask a moderator to change the settings or create your own host.";
                return RunPermission.None;
            }
            if (Access == AccessType.Ingame || Access == AccessType.IngameVote || Access == AccessType.IngameNotAutohost)
            {
                if (s.IsRunning)
                {
                    if (Access == AccessType.IngameVote) return RunPermission.Vote;
                    else return defPerm;
                }
                else
                {
                    reason = "This command can only be used while the game is running. Use !start to start the game.";
                    return RunPermission.None;
                }
            }
            if (Access == AccessType.NotIngame || Access == AccessType.NotIngameNotAutohost)
            {
                if (!s.IsRunning)
                {
                    return defPerm;
                }
                else
                {
                    reason = "This command can only be used outside of the game. Use !exit to abort the game.";
                    return RunPermission.None;
                }
            }

            //unknown access type
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

            /// <summary>
            /// Can be executed ingame/offgame by admins only
            /// </summary>
            [Description("At any time, by admins only, no vote needed")]
            Admin = 5,

            /// <summary>
            /// Can be executed not-ingame by non-spectators (vote) or admins or founder (direct). Unavailable to non-admins on autohosts
            /// </summary>
            [Description("When game not running, by players, might need a vote. Unavailable to non-admins on autohosts")]
            NotIngameNotAutohost = 6,

            /// <summary>
            /// Can be executed ingame by non-spectators (vote) or admins or founder (direct). Unavailable to non-admins on autohosts
            /// </summary>
            [Description("When game running, by players, might need a vote. Unavailable to non-admins on autohosts")]
            IngameNotAutohost = 7,
        }


        public enum RunPermission
        {
            None = 0,
            Vote = 1,
            Run = 2
        }


    }
}
