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
            using (var db = new ZkDataContext())
            {
                List<Resource> bannedMaps = db.AccountMapBans
                    .Where(x => x.AccountID == Global.AccountID)
                    .OrderBy(x => x.Rank)
                    .Select(x => x.Resource)
                    .ToList();

                var unusedBans = GlobalConst.MapBansPerPlayer - bannedMaps.Count;
                if (unusedBans > 0)
                {
                    // Add one blank resource for each ban the user has not applied yet to display in the view
                    bannedMaps.AddRange(Enumerable.Repeat(new Resource(), unusedBans));
                }
                else if (unusedBans < 0)
                {
                    // The user has more bans than currently allowed, likely because the global maximum was lowered
                    // after they saved their bans, so truncate the extra ones.
                    bannedMaps = bannedMaps.Take(GlobalConst.MapBansPerPlayer).ToList();
                }

                return View("MapBansIndex", bannedMaps);
            }
        }

        [Auth]
        /// <summary>
        /// Overrides current user's map bans.
        /// </summary>
        /// 

        public ActionResult Update(List<Resource> resources)
        {
            if (resources == null) return Content("No input given");

            var names = resources.Select(x => x.InternalName).ToList();
            // Duplicates would not break anything but they're probably a sign of user error so validate against them.
            var hasDuplicate = names.Where(x => x != null).GroupBy(x => x).Any(g => g.Count() > 1);
            if (hasDuplicate)
            {
                return Content("The same map cannot be banned multiple times.");
            }

            if (resources.Count > GlobalConst.MapBansPerPlayer)
            {
                return Content(String.Format("Cannot ban more than {0} maps, got {1} bans.", GlobalConst.MapBansPerPlayer, resources.Count));
            }

            // Fetch the actual resources to sanity check user input and populate IDs for newly selected maps.
            // Filter against current matchmaker to remove any existing bans for a map that has been removed from the MM pool.
            var db = new ZkDataContext();
            var mapIDs = db.Resources
                .Where(x => x.TypeID == ResourceType.Map && names.Contains(x.InternalName) && x.MapSupportLevel == MapSupportLevel.MatchMaker)
                .ToList();

            // Ban rank matters so sort the maps according to the user provided ordering
            mapIDs = mapIDs.OrderBy(x => names.IndexOf(x.InternalName)).ToList();

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
}