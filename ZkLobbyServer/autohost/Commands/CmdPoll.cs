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

        private BattleCommand commandToRun;

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
            commandToRun = battle.GetCommandByName(commandName);
            string reason;
            if (commandToRun.GetRunPermissions(battle, e.User, out reason) >= RunPermission.Vote && commandToRun.Access != AccessType.NoCheck)
            {
                return commandToRun.Arm(battle, e, commandArgs);
            }
            battle.Respond(e, reason);
            return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await commandToRun.ExecuteArmed(battle, e);
        }

        public override RunPermission GetRunPermissions(ServerBattle battle, string userName, out string reason)
        {
            if (commandToRun == null) return base.GetRunPermissions(battle, userName, out reason) >= RunPermission.Vote ? RunPermission.Vote : RunPermission.None;
            return commandToRun.GetRunPermissions(battle, userName, out reason);
        }
    }
}