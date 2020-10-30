using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZkLobbyServer
{
    public class CmdSetRatings : BattleCommand
    {
        private RatingCategory rating;
        public override string Help => "set applicable rating, allowed values are 1,2,4";
        public override string Shortcut => "setratings";
        public override AccessType Access => AccessType.Admin;

        public override BattleCommand Create() => new CmdSetRatings();

        public override string Arm(ServerBattle battle, Say e, string arguments = null)
        {
            int ratingInt;
            if (int.TryParse(arguments, out ratingInt) && Enum.IsDefined(typeof(RatingCategory), ratingInt))
            {
                rating = (RatingCategory)ratingInt;
                return $"Set applicable ratings to {rating}?";
            }
            else return null;
        }


        public override async Task ExecuteArmed(ServerBattle battle, Say e)
        {
            battle.SetApplicableRating(rating);
            await battle.SayBattle($"Applicable Rating changed to {rating}");
        }
    }
}