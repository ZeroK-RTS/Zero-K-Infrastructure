using System;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdPoll : BattleCommand
    {
        public override string Help => "- forces a poll, e.g. !poll map tabula";
        public override string Shortcut => "poll";
        public override AccessType Access => AccessType.NoCheck;
        public override BattleCommand Create() => new CmdPoll();
        public BattleCommand InternalCommand { get; private set; }

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                battle.Respond(e, "Pick a command to poll, e.g. !poll map tabula");
                return null;
            }
            var parts = arguments.Split(new[] { ' ' }, 2);
            var commandName = parts[0] ?? "";
            var commandArgs = parts.Length > 1 ? parts[1] : null;
            InternalCommand = battle.GetCommandByName(commandName);
            string reason;

            if (InternalCommand.GetRunPermissions(battle, e.User, out reason) >= RunPermission.Vote
            && InternalCommand.Access != AccessType.NoCheck
            && InternalCommand.Access != AccessType.Admin
            && InternalCommand.Access != AccessType.AdminOrRoomFounder
            && !(InternalCommand.Access == AccessType.NotIngameNotAutohost && battle.IsAutohost))
            {
                return InternalCommand.Arm(battle, e, commandArgs);
            }
            battle.Respond(e, reason);
            return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await InternalCommand.ExecuteArmed(battle, e);
        }

        public override RunPermission GetRunPermissions(ServerBattle battle, string userName, out string reason)
        {
            if (InternalCommand == null) return base.GetRunPermissions(battle, userName, out reason) >= RunPermission.Vote ? RunPermission.Vote : RunPermission.None;
            return InternalCommand.GetRunPermissions(battle, userName, out reason);
        }
    }
}
