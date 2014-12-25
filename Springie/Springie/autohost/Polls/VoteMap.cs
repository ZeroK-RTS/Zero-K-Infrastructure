using LobbyClient;
using ZkData.ContentService;

namespace Springie.autohost.Polls
{
    public class VoteMap: AbstractPoll
    {
        string map;

        public VoteMap(TasClient tas, Spring spring, AutoHost ah): base(tas, spring, ah) {}

        protected override bool PerformInit(TasSayEventArgs e, string[] words, out string question, out int winCount)
        {
            winCount = 0;
            question = null;
            if (spring.IsRunning)
            {
                AutoHost.Respond(tas, spring, e, "Cannot change map while the game is running");
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
                        map = vals[0];
                        var resource = ah.cache.FindResourceData(new string[]{map}, ResourceType.Map);
                        question = string.Format("Change map to {0} http://zero-k.info/Maps/Detail/{1} ?", map, resource[0].ResourceID);
                        return true;
                    }
                    else
                    {
                        AutoHost.Respond(tas, spring, e, "Cannot find such map");
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        if (tas.MyBattle != null && !spring.IsRunning)
                        {
                            var serv = new ZkData.SpringieInterfaceReference.SpringieService();
                            serv.Timeout = 15000;
                            serv.GetRecommendedMapCompleted += (sender, args) =>
                            {
                                if (!args.Cancelled && args.Error == null)
                                {
                                    var foundMap = args.Result;
                                    if (foundMap != null && foundMap.MapName != null && tas.MyBattle != null)
                                    {
                                        if (tas.MyBattle.MapName != foundMap.MapName)
                                        {
                                            map = foundMap.MapName;
                                        }
                                    }

                                }
                            };
                            serv.GetRecommendedMapAsync(tas.MyBattle.GetContext(), true);
                            // I have no idea why it can't just work like the above way
                            ResourceData[] resourceArray = ah.cache.FindResourceData(new string[] { map }, ResourceType.Map);
                            System.Collections.Generic.List<ResourceData> resourceList = new System.Collections.Generic.List<ResourceData>(resourceArray);
                            var resource = resourceList.Find(x => x.InternalName == map);
                            question = string.Format("Change map to {0} http://zero-k.info/Maps/Detail/{1} ?", map, resource.ResourceID);
                            return true;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        AutoHost.Respond(tas, spring, e, ex.ToString());
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