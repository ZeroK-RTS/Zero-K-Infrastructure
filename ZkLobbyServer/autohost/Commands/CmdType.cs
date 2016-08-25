using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdType : BattleCommand
    {
        AutohostMode mode = AutohostMode.None;
        public override string Help => $"[type] - changes room type, e.g. !type custom ({string.Join(", ", GetValidTypes().Select(x => x.Description()))})";
        public override string Shortcut => "type";
        public override AccessType Access => AccessType.NotIngame;

        private static List<AutohostMode> GetValidTypes() => Enum.GetValues(typeof(AutohostMode)).Cast<AutohostMode>().Where(x => x != AutohostMode.Planetwars).ToList();

        public override BattleCommand Create() => new CmdType();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            mode = GetValidTypes().FirstOrDefault(x => x.Description().Contains(arguments ?? ""));
            return $"Change room to {mode.Description()}?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            await battle.SwitchGameType(mode);
            await battle.SayBattle("Game type changed to " + mode.Description());
        }
    }
}