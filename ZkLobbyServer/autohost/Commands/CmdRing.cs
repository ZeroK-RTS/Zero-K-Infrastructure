using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class CmdRing : BattleCommand
    {
        public override string Help => "[<filters>..] - rings all unready or specific player(s), e.g. !ring - rings unready, !ring icho - rings Licho";
        public override string Shortcut => "ring";
        public override AccessType Access => AccessType.NoCheck;

        private List<string> userList;

        public override BattleCommand Create() => new CmdRing();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            userList = new List<string>();

            if (string.IsNullOrEmpty(arguments?.Trim()))
            {
                // ringing idle
                foreach (var p in battle.Users.Values)
                {
                    if (p.IsSpectator) continue;
                    var ingameEntry = battle.spring.Context.ActualPlayers.FirstOrDefault(x => x.Name == p.Name);
                    if ((p.SyncStatus != SyncStatuses.Synced || p.IsSpectator) || (battle.spring.IsRunning && ingameEntry?.IsSpectator == false && ingameEntry?.IsIngameReady == false)) userList.Add(p.Name);
                }
            }
            else userList = battle.GetAllUserNames().Where(x => x.Contains(arguments)).ToList();

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