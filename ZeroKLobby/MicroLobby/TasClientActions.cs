using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LobbyClient;

namespace ZeroKLobby.MicroLobby
{
    static class TasClientActions
    {
        public static void StartManualBattle(int battleID, string password)
        {
            Trace.TraceInformation("Joining battle {0}", battleID);
            var TasClient = Program.TasClient;
            if (TasClient.MyBattle != null)
            {
                Battle battle;
                if (TasClient.ExistingBattles.TryGetValue(battleID, out battle))
                {
                    TasClient.Say(SayPlace.Battle, "", string.Format("Going to {0} zk://@join_player:{1}", battle.Title, battle.Founder.Name), true);
                }
                TasClient.LeaveBattle();
            }
            if (!string.IsNullOrEmpty(password)) Program.TasClient.JoinBattle(battleID, password);
            else Program.TasClient.JoinBattle(battleID);
        }

        public static void LeaveBattle()
        {
            Trace.TraceInformation("Closing current battle");
            Program.TasClient.LeaveBattle();
            Program.MainWindow.navigationControl.Path = "battles";
        }
    }
}
