using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;

namespace Springie.autohost
{
    public class MatchMakerQueue
    {
        static string[] allowedCommands = new[] { "map", "help", "ring", "vote", "saveboxes", "clearbox", "addbox", "endvote", "maplink", "y", "n", "votemap", "votekick", "kick", "corners", "split" };


        List<string> userOrder = new List<string>();
        TasClient tas;

        bool starting;
        DateTime startingFrom;
        DateTime scheduledStart;

        public MatchMakerQueue(AutoHost ah)
        {
            ah.Commands.Commands.RemoveAll(x => !allowedCommands.Contains(x.Name));

            tas = ah.tas;

            tas.BattleUserJoined += (sender, args) =>
            {
                if (tas.MyBattleID != args.BattleID) return;
                userOrder.Add(args.UserName); tas.Say(TasClient.SayPlace.BattlePrivate, args.UserName, string.Format("Hi {0}, you are {1}. in the queue", args.UserName, userOrder.Count), true);


                var count = tas.MyBattle.Users.Count - 1;
                if (count >= ah.config.MinToJuggle)
                {
                    if (!starting)
                    {
                        startingFrom = DateTime.Now;
                        scheduledStart = startingFrom.AddMinutes(1); // start in one minute
                        starting = true;
                    }
                    else
                    {
                        var postpone = scheduledStart.AddMinutes(1);
                        var deadline = startingFrom.AddMinutes(3);
                        if (postpone > deadline) scheduledStart = deadline;
                        else scheduledStart = postpone;
                    }
                    tas.Say(TasClient.SayPlace.Battle, "", string.Format("Queue starting in {0}s", Math.Round(scheduledStart.Subtract(DateTime.Now).TotalSeconds)), true);
                }
                else
                {
                    tas.Say(TasClient.SayPlace.Battle, "", string.Format("Queue needs {0} more people", ah.config.MinToJuggle - count), true);
                }
            };

            tas.BattleUserLeft += (sender, args) =>
            {
                if (tas.MyBattleID != args.BattleID) return;
                userOrder.Remove(args.UserName);

                var count = tas.MyBattle.Users.Count - 1;
                if (count < ah.config.MinToJuggle)
                {
                    starting = false;
                }
                tas.Say(TasClient.SayPlace.Battle, "", string.Format("Queue needs {0} more people", ah.config.MinToJuggle - count), true);
            };


        }
    }
}