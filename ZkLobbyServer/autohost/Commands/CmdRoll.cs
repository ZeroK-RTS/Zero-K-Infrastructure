using System;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdRoll : BattleCommand
    {
        public override string Help => "<N> - rolls a 1dN sided die";
        public override string Shortcut => "roll";
        public override AccessType Access => AccessType.NotIngame;

        private int maximum;

        public override BattleCommand Create() => new CmdRoll();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            if (int.TryParse(arguments, out maximum)) return string.Empty;
            else return null;
        }

        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            if (maximum >= 1)
            {
                var result = new Random().Next(1, maximum);
                await battle.SayBattle($"Rolled 1d{maximum}, got {result}");
            }
        }
    }
}
