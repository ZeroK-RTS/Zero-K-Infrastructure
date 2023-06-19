using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using PlasmaShared;
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
                await battle.SayBattle("The following users are still downloading the map, please click Rejoin ASAP because you're playing: " + unready.Select(x => x.Name).StringJoin());
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
        public override int GetPollWinMargin(ServerBattle battle, int numVoters)
        {
            // Require one more vote to start a game with uneven teams so at least one player in the smaller
            // team needs to agree to start the game. This is particularly relevant for the 2v1 case.
            if (battle.Mode == PlasmaShared.AutohostMode.Teams) {
                return base.GetPollWinMargin(battle, numVoters) + numVoters % 2;
            } else
            {
                return base.GetPollWinMargin(battle, numVoters);
            }
        }

        private async Task StartGame(ServerBattle battle)
        {
            await battle.RunCommandDirectly<CmdSpecAfk>(null);
            await battle.StartGame();
        }
    }
}
