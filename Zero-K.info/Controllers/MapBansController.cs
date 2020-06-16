using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class MapBansController : Controller
    {
        [Auth]
        /// <summary>
        /// Returns current user's map bans and allows editing them.
        /// </summary>
        /// 
        public ActionResult Index()
        {
            var db = new ZkDataContext();
            List<AccountMapBan> bans = db.AccountMapBans
                .Where(x => x.AccountID == Global.AccountID)
                .OrderBy(x => x.Rank)
                .ToList();

            var viewModel = new MapBansViewModel
            {
                accountBans = bans,
                maxBans = MapBanConfig.GetMaxBanCount()
            };

            return View("MapBansIndex", viewModel);
        }

        [Auth]
        /// <summary>
        /// Overrides current user's map bans.
        /// </summary>
        /// 

        public ActionResult Update(List<String> mapName)
        {
            if (mapName == null) return Content("No input given");

            // Duplicates would not break anything but they're probably a sign of user error so validate against them.
            var hasDuplicate = mapName.Where(x => x != "").GroupBy(x => x).Any(g => g.Count() > 1);
            if (hasDuplicate)
            {
                return Content("The same map cannot be banned multiple times.");
            }

            // TODO: It would be nicer to use IDs instead of the map names to drive this
            // Fetch the actual resources to sanity check user input and silently ignore
            // any input that does not actually exist.
            // Filter against current matchmaker to remove any existing bans for a map 
            // that has been removed from the MM pool.
            var db = new ZkDataContext();
            var mapIDs = db.Resources
                .Where(x => x.TypeID == ResourceType.Map && mapName.Contains(x.InternalName) && x.MapSupportLevel == MapSupportLevel.MatchMaker)
                .ToList();

            // Ban rank matters so sort the maps according to the user provided ordering
            mapIDs = mapIDs.OrderBy(x => mapName.IndexOf(x.InternalName)).ToList();

            var newMapBans = new List<AccountMapBan>();
            for (int i = 0; i < mapIDs.Count; i++)
            {
                var ban = new AccountMapBan
                {
                    Rank = i + 1,
                    AccountID = Global.AccountID,
                    BannedMapResourceID = mapIDs[i].ResourceID
                };
                newMapBans.Add(ban);
            }

            // Since users may only have a few bans and will not often update them,
            // recreate them from scratch on submission. This shortcut simplifies handling
            // partial updates that modify a ban's rank and handling multiple bans for the same map.
            db.AccountMapBans.RemoveRange(db.AccountMapBans.Where(x => x.AccountID == Global.AccountID));
            db.AccountMapBans.InsertAllOnSubmit(newMapBans);
            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }

    public class MapBansViewModel
    {
        public List<AccountMapBan> accountBans;
        public int maxBans;
    }
}