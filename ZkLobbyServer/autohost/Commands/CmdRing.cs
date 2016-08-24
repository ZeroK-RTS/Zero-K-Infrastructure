using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkLobbyServer.autohost;

namespace ZkLobbyServer
{
    public class CmdRing : ServerBattleCommand
    {
        public override string Help => "[<filters>..] - rings all unready or specific player(s), e.g. !ring - rings unready, !ring icho - rings Licho";
        public override string Shortcut => "ring";
        public override BattleCommandAccess Access => BattleCommandAccess.NoCheck;

        private List<string> userList;

        public override ServerBattleCommand Create() => new CmdRing();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            userList = new List<string>();

            if (string.IsNullOrEmpty(arguments?.Trim()))
            {
                // ringing idle
                foreach (var p in battle.Users.Values)
                {
                    if (p.IsSpectator) continue;
                    if ((p.SyncStatus != SyncStatuses.Synced || p.IsSpectator) && (!battle.spring.IsRunning || !battle.spring.IsPlayerReady(p.Name))) userList.Add(p.Name);
                }
            }
            else
            {
                string[] vals;
                int[] indexes;
                battle.FilterUsers(arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), out vals, out indexes);
                userList = new List<string>(vals);
            }

            return $"do you want to ring {userList.Count} players?";
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            foreach (var s in userList)
            {
                await battle.server.GhostSay(new Say() { User = e.User, Target = s, Text = e.User + " wants your attention", IsEmote = true, Ring = true, Place = SayPlace.User });
            }
        }
    }
}