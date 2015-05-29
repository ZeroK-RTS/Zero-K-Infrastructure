using System;
using System.Threading;
using LobbyClient;
using System.Linq;
using System.Collections.Generic;

namespace Springie.autohost.Polls
{
    public class VoteShuffleBox : AbstractPoll
    {
        bool shuffleBox;

        public VoteShuffleBox(TasClient tas, Spring spring, AutoHost ah) : base(tas, spring, ah) { }

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;

            if (tas.MyBattle == null) return false;

            if (spring.IsRunning)
            {
                AutoHost.Respond(tas, spring, e, "Cannot set options while the game is running");
                return false;
            }
            if (words.Length != 1 || !(words[0] == "all" || words[0] == "occupied" || words[0] == "off"))
            {
                AutoHost.Respond(tas, spring, e, "Parameters must be \"off\" \"all\" or \"occupied\"");
                return false;
            }

            switch (words[0])
            {
                case "all":
                    if (tas.MyBattle.RngEveryBox)
                    {
                        AutoHost.Respond(tas, spring, e, "Shuffling of all start-box is already on");
                        return false;
                    }
                    question = "Activate shuffling of all start-box?";
                    tas.MyBattle.RngEveryBox = true;
                    tas.MyBattle.RngActiveBox = false;
                    break;
                case "occupied":
                    if (tas.MyBattle.RngActiveBox)
                    {
                        AutoHost.Respond(tas, spring, e, "Shuffling of occupied start-boxes is already on");
                        return false;
                    }
                    question = "Activate shuffling of occupied start-boxes?";
                    tas.MyBattle.RngEveryBox = false;
                    tas.MyBattle.RngActiveBox = true;
                    break;
                default:
                    if (!(tas.MyBattle.RngEveryBox || tas.MyBattle.RngActiveBox))
                    {
                        AutoHost.Respond(tas, spring, e, "Shuffling of start-boxes is already off");
                        return false;
                    }
                    question = "Deactivate shuffling of start-boxes?";
                    tas.MyBattle.RngEveryBox=false;
                    tas.MyBattle.RngActiveBox = false;
                    break;
            }
            return true;
        }

        protected override void SuccessAction()
        {
            ah.ComShuffleBox(TasSayEventArgs.Default, new string[] { shuffleBox?"1":"0" });
        }
    }
}
