using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;
using System.Linq;
using ZkLobbyServer;

namespace Springie.autohost.Polls
{
    public class VoteMap: AbstractPoll
    {
        string map;

        public VoteMap(Spring spring, ServerBattle ah): base(spring, ah) {}

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
                string[] vals;
                int[] indexes;
                if (words.Length > 0)
                {
                    ah.FilterMaps(words, out vals, out indexes);
                    if (vals.Length > 0)
                    {
                        foreach (string possibleMap in vals)
                        {
                            map = possibleMap;
                            var resourceList = ah.cache.FindResourceData(new string[] { map }, ResourceType.Map);
                            if (resourceList != null)
                            {
                            	var resource = resourceList[0];
                                question = string.Format(
                                    "Change map to {0} {2}/Maps/Detail/{1} ?",
                                    map,
                                    resource.ResourceID,
                                    GlobalConst.BaseSiteUrl);
                                return true;
                            }
                        }
                        ah.Respond(e, "Cannot find such map");
                        return false;
                    }
                    else
                    {
                        ah.Respond(e, "Cannot find such map");
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        if (!spring.IsRunning)
                        {
                            var serv = GlobalConst.GetSpringieService();
                            Task.Factory.StartNew(() => {
                                RecommendedMapResult foundMap;
                                try {
                                    foundMap = serv.GetRecommendedMap(ah.GetContext(), true);
                                } catch (Exception ex) {
                                    Trace.TraceError(ex.ToString());
                                    return;
                                }
                                if (foundMap != null && foundMap.MapName != null) {
                                    if (ah.MapName != foundMap.MapName) {
                                        map = foundMap.MapName;
                                    }
                                }
                            });

                            
                            // I have no idea why it can't just work like the above way
                            var resourceList = ah.cache.FindResourceData(new string[] { map }, ResourceType.Map);
                            var resource = resourceList.Find(x => x.InternalName == map);
                            if (resource != null)
                            {
                                question = string.Format("Change map to {0} {2}/Maps/Detail/{1} ?", map, resource.ResourceID, GlobalConst.BaseSiteUrl);
                                return true;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ah.Respond(e, ex.ToString());
                        //System.Diagnostics.Trace.TraceError(ex.ToString());
                    }
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
