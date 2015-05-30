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
            int x;
            if (words.Length != 1 || !Int32.TryParse(words[0], out x))
            {
                AutoHost.Respond(tas, spring, e, "Parameters must be a 1 or 0");
                return false;
            }
            //Already voted for?
            if (x > 0 && tas.MyBattle.ShuffleBox)
            {
                AutoHost.Respond(tas, spring, e, "Startbox shuffling already active.");
                return false;
            }
            else if (x <= 0 && !tas.MyBattle.ShuffleBox)
            {
                AutoHost.Respond(tas, spring, e, "Startbox shuffling already off.");
                return false;
            }

            question = (x > 0)?"Activate startbox shuffling":"Deactivate startbox shuffling";
            shuffleBox = (x > 0);
            return true;
        }

        protected override void SuccessAction()
        {
            ah.ComShuffleBox(TasSayEventArgs.Default, new string[] { shuffleBox?"1":"0" });
        }
    }
}
