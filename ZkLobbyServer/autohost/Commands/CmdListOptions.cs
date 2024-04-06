using System;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdListOptions : BattleCommand
    {
        public override string Help => "lists all game options";
        public override string Shortcut => "listoptions";
        public override AccessType Access => AccessType.NoCheck;

        public override BattleCommand Create() => new CmdListOptions();

        public override string Arm(ServerBattle battle, Say e, string arguments = null) => String.Empty;

        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var mod = battle.HostedModInfo;
            if (mod == null || mod.Options.Length == 0) await battle.Respond(e, "this game has no options");
            else foreach (var opt in mod.Options) await battle.Respond(e, opt.ToString());
        }
    }
}