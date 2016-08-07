using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;
using System.Linq;
using ZeroKWeb.SpringieInterface;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteMap : AbstractPoll
    {
        string map;

        public VoteMap(Spring spring, ServerBattle ah) : base(spring, ah) { }

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                ah.Respond(e, "Cannot change map while the game is running");
                return false;
            }
            else
            {
                Resource mapResource;
                if (words.Length > 0)
                {
                    mapResource = MapPicker.FindResources(ResourceType.Map, words).Take(ServerBattle.MaxMapListLength).FirstOrDefault();
                }
                else
                {
                    mapResource = MapPicker.GetRecommendedMap(ah.GetContext());
                }
                if (mapResource != null)
                {
                    map = mapResource.InternalName;
                    question = string.Format("Change map to {0} {2}/Maps/Detail/{1} ?", mapResource.InternalName, mapResource.ResourceID, GlobalConst.BaseSiteUrl);
                    return true;
                }
                else
                {
                    ah.Respond(e, "Cannot find such map");
                    return false;
                }
            }
        }

        protected override void SuccessAction()
        {
            if (string.IsNullOrEmpty(map))
            {
                ah.ComMap(TasSayEventArgs.Default, new string[] { });
            }
            else
            {
                ah.ComMap(TasSayEventArgs.Default, new string[] { map });
            }
        }
    }
}
