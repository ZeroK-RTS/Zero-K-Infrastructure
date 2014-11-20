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

        public MatchMakerQueue(AutoHost ah)
        {
            ah.Commands.Commands.RemoveAll(x => !allowedCommands.Contains(x.Name));

            tas = ah.tas;

            tas.BattleUserJoined += (sender, args) =>
            {
                if (tas.MyBattleID != args.BattleID) return;
                userOrder.Add(args.UserName); tas.Say(TasClient.SayPlace.BattlePrivate, args.UserName, string.Format("Hi {0}, you are {1}. in the queue", args.UserName, userOrder.Count), true);
            };

            tas.BattleUserLeft += (sender, args) =>
            {
                if (tas.MyBattleID != args.BattleID) return;
                userOrder.Remove(args.UserName);
            };

        }
    }
}