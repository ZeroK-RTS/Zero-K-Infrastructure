using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class CmdStart : BattleCommand
    {
        public override string Help => "starts the game";
        public override string Shortcut => "start";
        public override AccessType Access => AccessType.NotIngame;

        public override BattleCommand Create() => new CmdStart();

        private Timer startTimer;

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            battle.RunCommandDirectly<CmdRing>(e);
            return $"Start the game?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            var afkers = battle.Users.Values.Where(x => !x.IsSpectator && x.LobbyUser.IsAway);
            var unready = battle.Users.Values.Where(x => !x.IsSpectator && x.SyncStatus != SyncStatuses.Synced);
            bool wait = false;
            if (afkers.Any())
            {
                wait = true;
                await battle.SayBattle("The following users are afk and will be spectated: " + afkers.Select(x => x.Name).StringJoin());
            }
            if (unready.Any())
            {
                wait = true;
                await battle.SayBattle("The following users are still downloading the map: " + unready.Select(x => x.Name).StringJoin());
            }
            if (wait)
            {
                await battle.SayBattle("Game starting in 10 seconds...");
                battle.BlockPolls(10);
                startTimer = new Timer(10000);
                startTimer.Enabled = true;
                startTimer.AutoReset = false;
                startTimer.Elapsed += (t, s) => { StartGame(battle); };
            }
            else
            {
                await StartGame(battle);
            }

        }

        private async Task StartGame(ServerBattle battle)
        {
            await battle.RunCommandDirectly<CmdSpecAfk>(null);
            await battle.StartGame();
        }
    }
}
