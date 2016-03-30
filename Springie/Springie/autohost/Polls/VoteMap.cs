using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;
using System.Linq;

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
                        bool serious = ah.config.Mode == AutohostMode.Serious;
                        var db = serious ? new ZkDataContext() : null;

                        foreach (string possibleMap in vals)
                        {
                            if (serious)
                            {
                                try
                                {
                                    var mapEntry = db.Resources.FirstOrDefault(x => x.InternalName == possibleMap);
                                    if (mapEntry != null && mapEntry.MapIsSpecial == true) continue;
                                }
                                catch (Exception ex) { }    // meh
                            }

                            map = possibleMap;
                            var resource = ah.cache.FindResourceData(new string[] { map }, ResourceType.Map);
                            if (resource != null)
                            {
                                question = string.Format(
                                    "Change map to {0} {2}/Maps/Detail/{1} ?",
                                    map,
                                    resource[0].ResourceID,
                                    GlobalConst.BaseSiteUrl);
                                return true;
                            }
                        }
                        AutoHost.Respond(tas, spring, e, String.Format("Cannot find such {0}map",serious ? "(non-special) " : ""));
                        return false;
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
                            var serv = GlobalConst.GetSpringieService();
                            Task.Factory.StartNew(() => {
                                RecommendedMapResult foundMap;
                                try {
                                    foundMap = serv.GetRecommendedMap(tas.MyBattle.GetContext(), true);
                                } catch (Exception ex) {
                                    Trace.TraceError(ex.ToString());
                                    return;
                                }
                                if (foundMap != null && foundMap.MapName != null && tas.MyBattle != null) {
                                    if (tas.MyBattle.MapName != foundMap.MapName) {
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

         protected override bool AllowVote(TasSayEventArgs e)
        {
            if (tas.MyBattle == null) return false;
            var entry = tas.MyBattle.Users.Values.FirstOrDefault(x => x.Name == e.UserName);
            if (entry == null || entry.IsSpectator)
            {
                ah.Respond(e, string.Format("Only players can vote"));
                return false;
            }
            else return true;
        }
    }
}
