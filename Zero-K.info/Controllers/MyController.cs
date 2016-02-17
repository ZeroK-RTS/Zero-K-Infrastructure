using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web.Mvc;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZeroKWeb.Controllers
{
    /// <summary>
    /// Handles personal stuff like <see cref="Commander"/> designs
    /// </summary>
	public class MyController: Controller
	{
		//
		// GET: /My/

        public static bool IsUnlockValidForSlot(Unlock unlock, CommanderSlot slot)
        {
            if (slot.UnlockType == UnlockTypes.WeaponBoth)
            {
                if (unlock.UnlockType != UnlockTypes.Weapon && unlock.UnlockType != UnlockTypes.WeaponManualFire)
                    return false;
            }
            else if (unlock.UnlockType != slot.UnlockType)
                return false;

            if (unlock.MorphLevel > slot.MorphLevel) return false;

            return true;
        }

        public static bool IsPrerequisiteUnlockPresent(Commander comm, Unlock unlock)
        {
            if (!string.IsNullOrEmpty(unlock.RequiredInstalledUnlockIDs))
            {
                var requiredUnlockIDs = unlock.RequiredInstalledUnlockIDs.Split(',').Select(int.Parse);
                if (!comm.CommanderModules.Any(x => requiredUnlockIDs.Contains(x.ModuleUnlockID)))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Add, modify or delete a <see cref="Commander"/>
        /// </summary>
        /// <param name="profileNumber">The <see cref="Commander"/> profile number on the page (1-6)</param>
        /// <param name="name"></param>
        /// <param name="chassis">The <see cref="Unlock"/> ID of the commander chassis to use</param>
        /// <param name="deleteCommander">If not null or empty, delete the <see cref="Commander"/></param>
        /// <returns></returns>
		[Auth]
		public ActionResult CommanderProfile(int profileNumber, string name, int? chassis, string deleteCommander)
		{
			if (profileNumber < 1 || profileNumber > GlobalConst.CommanderProfileCount) return Content("WTF! get lost");

			var db = new ZkDataContext();
			using (var scope = new TransactionScope())
			{

				var unlocks = db.AccountUnlocks.Where(x => x.AccountID == Global.AccountID);

				Commander comm = db.Commanders.SingleOrDefault(x => x.ProfileNumber == profileNumber && x.AccountID == Global.AccountID);
				if (comm != null)
				{
					if (!string.IsNullOrEmpty(deleteCommander)) // delete commander
					{
						db.Commanders.DeleteOnSubmit(comm);
						db.SubmitChanges();
						scope.Complete();
						return GetCommanderProfileView(db, profileNumber);
					}
				}
				else
				{
					comm = new Commander() { AccountID = Global.AccountID, ProfileNumber = profileNumber };
					db.Commanders.InsertOnSubmit(comm);
				}

				if (comm.Unlock == null)
				{
					var chassisUnlock = unlocks.FirstOrDefault(x => x.UnlockID == chassis);
					if ((chassis == null || chassisUnlock == null)) return GetCommanderProfileView(db, profileNumber);
					else
					{
						comm.ChassisUnlockID = chassis.Value;
						comm.Unlock = chassisUnlock.Unlock;
					}
				}

                if (!string.IsNullOrEmpty(name))
                {
                    if (name.Length > GlobalConst.MaxCommanderNameLength) name = name.Substring(0, GlobalConst.MaxCommanderNameLength);
                    name = Regex.Replace(name, @"[^\u0000-\u007F]", string.Empty); // remove unicode stuff
                    comm.Name = name;
                }

                // process modules
				foreach (var key in Request.Form.AllKeys.Where(x => !string.IsNullOrEmpty(x)))
				{
					var m = Regex.Match(key, "m([0-9]+)");
					if (m.Success)
					{
						var slotId = int.Parse(m.Groups[1].Value);
						int unlockId;
						int.TryParse(Request.Form[key], out unlockId);

						if (unlockId > 0)
						{
							CommanderSlot slot = db.CommanderSlots.Single(x => x.CommanderSlotID == slotId);
							Unlock unlock = db.Unlocks.Single(x => x.UnlockID == unlockId);

							if (!unlocks.Any(x => x.UnlockID == unlock.UnlockID)) return Content("WTF get lost!");
							if (slot.MorphLevel < unlock.MorphLevel || !IsUnlockValidForSlot(unlock, slot)) return Content(string.Format("WTF cannot use {0} in slot {1}", unlock.Name, slot.CommanderSlotID));
							if (!string.IsNullOrEmpty(unlock.LimitForChassis))
							{
								var validChassis = unlock.LimitForChassis.Split(',');
								if (!validChassis.Contains(comm.Unlock.Code)) return Content(string.Format("{0} cannot be used in commander {1}", unlock.Name, comm.Unlock.Name));
							}
                            if (!IsPrerequisiteUnlockPresent(comm, unlock))
                                return Content(string.Format("{0} missing prerequisite module", unlock.Name));

							var comSlot = comm.CommanderModules.SingleOrDefault(x => x.SlotID == slotId);
							if (comSlot == null)
							{
								comSlot = new CommanderModule() { SlotID = slotId };
								comm.CommanderModules.Add(comSlot);
							}
							comSlot.ModuleUnlockID = unlockId;
						}
						else
						{
							var oldModule = comm.CommanderModules.FirstOrDefault(x => x.SlotID == slotId);
							if (oldModule != null) comm.CommanderModules.Remove(oldModule);
						}
					}
				}

                // process decorations
                foreach (var key in Request.Form.AllKeys.Where(x => !string.IsNullOrEmpty(x)))
                {
                    var d = Regex.Match(key, "d([0-9]+)");
                    if (d.Success)
                    {
                        var slotId = int.Parse(d.Groups[1].Value);
                        int unlockId;
                        int.TryParse(Request.Form[key], out unlockId);

                        if (unlockId > 0)
                        {
                            CommanderDecorationSlot decSlot = db.CommanderDecorationSlots.Single(x => x.CommanderDecorationSlotID == slotId);
                            Unlock unlock = db.Unlocks.Single(x => x.UnlockID == unlockId);

                            if (!unlocks.Any(x => x.UnlockID == unlock.UnlockID)) return Content("WTF get lost!");
                            if (!string.IsNullOrEmpty(unlock.LimitForChassis))
                            {
                                var validChassis = unlock.LimitForChassis.Split(',');
                                if (!validChassis.Contains(comm.Unlock.Code)) return Content(string.Format("{0} cannot be used in commander {1}", unlock.Name, comm.Unlock.Name));
                            }

                            var comSlot = comm.CommanderDecorations.SingleOrDefault(x => x.SlotID == slotId);
                            if (comSlot == null)
                            {
                                comSlot = new CommanderDecoration() { SlotID = slotId };
                                comm.CommanderDecorations.Add(comSlot);
                            }
                            comSlot.DecorationUnlockID = unlockId;
                        }
                        else
                        {
                            var oldDecoration = comm.CommanderDecorations.FirstOrDefault(x => x.SlotID == slotId);
                            if (oldDecoration != null) comm.CommanderDecorations.Remove(oldDecoration);
                        }
                    }
                }

                // remove a module/decoration if ordered to
                foreach (var toDel in Request.Form.AllKeys.Where(x => !string.IsNullOrEmpty(x)))
                {
					var m = Regex.Match(toDel, "deleteSlot([0-9]+)");
					if (m.Success)
					{
						var slotId = int.Parse(m.Groups[1].Value);
						comm.CommanderModules.Remove(comm.CommanderModules.SingleOrDefault(x => x.SlotID == slotId));
					}
                
                    var d = Regex.Match(toDel, "deleteDecorationSlot([0-9]+)");
                    if (d.Success)
                    {
                        var decSlotId = int.Parse(d.Groups[1].Value);
                        comm.CommanderDecorations.Remove(comm.CommanderDecorations.SingleOrDefault(x => x.SlotID == decSlotId));
                    }
                }

                // cleanup invalid modules?


				db.SubmitChanges();
				foreach (var unlock in comm.CommanderModules.GroupBy(x => x.Unlock))
				{
					if (unlock.Key == null) continue;
					var owned = unlocks.Where(x => x.UnlockID == unlock.Key.UnlockID).Sum(x => (int?)x.Count) ?? 0;
					if (owned < unlock.Count())
					{
						var toRemove = unlock.Count() - owned;

						foreach (var m in unlock.OrderByDescending(x => x.SlotID))
						{
							db.CommanderModules.DeleteOnSubmit(m);
							//comm.CommanderModules.Remove(m);
							toRemove--;
							if (toRemove <= 0) break;
						}
					}
				}

				db.SubmitChanges();
				scope.Complete();
			}

			return GetCommanderProfileView(db, profileNumber);
		}

        /// <summary>
        /// Go to the <see cref="Commander"/> configuration page
        /// </summary>
		[Auth]
		public ActionResult Commanders()
		{
			var db = new ZkDataContext();

			var ret = new CommandersModel();
			ret.Unlocks =
				GetUserUnlockCountsListIncludingFree(db).Where(
					x => x.Unlock.UnlockType != UnlockTypes.Unit).ToList
					();
			ret.Account = Global.Account;
			return View(ret);
		}

		public ActionResult Index()
		{
			return RedirectToAction("Detail", "Users", new { id = Global.AccountID });
		}

        /// <summary>
        /// Reset all the user's unlocks
        /// </summary>
		public ActionResult Reset()
		{
			var db = new ZkDataContext();
			db.AccountUnlocks.DeleteAllOnSubmit(db.AccountUnlocks.Where(x => x.AccountID == Global.AccountID));
			db.SubmitChanges();
			return RedirectToAction("UnlockList");
		}

        /// <summary>
        /// Unlock the specified <see cref="Unlock"/> for the current user
        /// </summary>
        /// <param name="id">The ID of the <see cref="Unlock"/> to unlock</param>
        [Auth]
        public ActionResult Unlock(int id, bool useKudos = false)
        {
            using (var db = new ZkDataContext())
            using (var scope = new TransactionScope())
            {

                List<Unlock> unlocks;
                List<Unlock> future;

                GetUnlockLists(db, out unlocks, out future);

                if (unlocks.Any(x => x.UnlockID == id))
                {
                    Unlock unlock = db.Unlocks.FirstOrDefault(x => x.UnlockID == id);
                    if (!useKudos && unlock.IsKudosOnly == true) return Content("That unlock cannot be bought using XP");

                    if (useKudos) {
                        var acc = db.Accounts.Find(Global.AccountID);
                        if (acc.Kudos < unlock.KudosCost) return Content("Not enough kudos to unlock this");
                        acc.KudosPurchases.Add(new KudosPurchase() {Time = DateTime.UtcNow, Unlock = unlock, Account = acc, KudosValue = unlock.KudosCost??0});
                        db.SaveChanges();
                        acc.Kudos = acc.KudosGained - acc.KudosSpent;
                        db.SaveChanges();
                    }
                    
                    var au = db.AccountUnlocks.SingleOrDefault(x => x.AccountID == Global.AccountID && x.UnlockID == id);
                    if (au == null)
                    {
                        au = new AccountUnlock() { AccountID = Global.AccountID, UnlockID = id, Count = 1 };
                        db.AccountUnlocks.InsertOnSubmit(au);
                    }
                    else au.Count++;
                    db.SaveChanges();
                }
                scope.Complete();
            }
            return RedirectToAction("UnlockList");
        }

        /// <summary>
        /// List all <see cref="Unlock"/>s, divided into what we can unlock and what is currently inaccessible to us
        /// </summary>
		[Auth]
		public ActionResult UnlockList()
		{
			List<Unlock> unlocks;
			List<Unlock> future;
			var db = new ZkDataContext();
			GetUnlockLists(db, out unlocks, out future);

			return View("UnlockList", new UnlockListResult() { Account = Global.Account, Unlocks = unlocks, FutureUnlocks = future, AlreadyUnlockedCounts = GetUserUnlockCountsListIncludingFree(db) });
		}

		PartialViewResult GetCommanderProfileView(ZkDataContext db, int profile)
		{
			var com = db.Commanders.SingleOrDefault(x => x.AccountID == Global.AccountID && x.ProfileNumber == profile);

			return PartialView("CommanderProfile",
			                   new CommanderProfileModel
			                   {
			                   	ProfileID = profile,
			                   	Commander = com,
			                   	Slots = db.CommanderSlots.ToList(),
                                DecorationSlots = db.CommanderDecorationSlots.ToList(),
			                   	Unlocks =
									GetUserUnlockCountsListIncludingFree(db).Where(
			                   			x =>
			                   			(x.Unlock.UnlockType != UnlockTypes.Unit)).ToList().Where(
			                   			 	x =>
			                   			 	(com == null || x.Unlock.LimitForChassis == null || x.Unlock.LimitForChassis.Contains(com.Unlock.Code)) &&
							   			 	(com == null || x.Count > com.CommanderModules.Count(y => y.ModuleUnlockID == x.Unlock.UnlockID)) &&
											(com == null || x.Count > com.CommanderDecorations.Count(y => y.DecorationUnlockID == x.Unlock.UnlockID))
											).ToList()
			                   });
		}

		Dictionary<Unlock, int> GetUserUnlockCountsDictIncludingFree(ZkDataContext db)
		{
			Dictionary<ZkData.Unlock, int> unlocks = Global.Account.AccountUnlocks.Select(x => new {x.Unlock, x.Count}).ToDictionary(x=> x.Unlock, x=> x.Count);
			foreach (Unlock unlock in db.Unlocks.Where(x=> x.XpCost <= 0 && x.NeededLevel <= Global.Account.Level && x.IsKudosOnly == false))
			{
				unlocks.Add(unlock, unlock.MaxModuleCount);
			}
			return unlocks;
		}

		List<UnlockCountEntry> GetUserUnlockCountsListIncludingFree(ZkDataContext db)
		{
			return GetUserUnlockCountsDictIncludingFree(db).Select(x => new UnlockCountEntry() {Unlock = x.Key, Count = x.Value}).ToList();
		}

        /// <summary>
        /// Get all <see cref="Unlock"/>s that the user can buy or could buy in the future
        /// </summary>
        /// <param name="db"></param>
        /// <param name="unlocks">This stores the unlocks we can get now (sufficient XP, level, kudos, etc.)</param>
        /// <param name="future">This stores everything we don't have that is not in the previous list</param>
		void GetUnlockLists(ZkDataContext db, out List<Unlock> unlocks, out List<Unlock> future)
		{
			// unlocks we already have the maximum of
			var maxedUnlockSet =
				new HashSet<int>(db.AccountUnlocks.Where(x => x.AccountID == Global.AccountID && x.Count >= x.Unlock.MaxModuleCount).Select(x => x.UnlockID));
			// unlocks we already have at least one of
			var anyUnlockSet = new HashSet<int>(db.AccountUnlocks.Where(x => x.AccountID == Global.AccountID && x.Count > 0).Select(x => x.UnlockID));

			var freeUnlocks = db.Unlocks.Where(x => x.XpCost <= 0 && x.IsKudosOnly == false).Select(x=> x.UnlockID);
			maxedUnlockSet.UnionWith(freeUnlocks);
			anyUnlockSet.UnionWith(freeUnlocks);

			var temp =
				db.Unlocks.Where(
					x =>
					x.NeededLevel <= Global.Account.Level && !maxedUnlockSet.Contains(x.UnlockID)
                    && ((x.KudosCost != null && x.KudosCost <= Global.Account.Kudos) || ((x.IsKudosOnly == null || x.IsKudosOnly == false) && x.XpCost <= Global.Account.AvailableXP))
					&& (x.RequiredUnlockID == null || anyUnlockSet.Contains(x.RequiredUnlockID ?? 0))
                    ).OrderBy(x => x.NeededLevel).ThenBy(x => x.XpCost).ThenBy(x => x.UnlockType).ToList();
			unlocks = temp;
		    var tempList = temp.Select(y => y.UnlockID).ToList();

			future =
				db.Unlocks.Where(x => !maxedUnlockSet.Contains(x.UnlockID) && !tempList.Contains(x.UnlockID)).OrderBy(x => x.NeededLevel).
					ThenBy(x => x.XpCost).ThenBy(x => x.Name).ToList();
		}


		public class CommanderProfileModel
		{
			public Commander Commander;
			public int ProfileID;
			public List<CommanderSlot> Slots;
            public List<CommanderDecorationSlot> DecorationSlots;
			public List<UnlockCountEntry> Unlocks;
		}

		public class CommandersModel
		{
			public Account Account;
			public List<UnlockCountEntry> Unlocks;
		}


		public class UnlockListResult
		{
			public Account Account;
			public IEnumerable<Unlock> FutureUnlocks;
			public IEnumerable<Unlock> Unlocks;
			public List<UnlockCountEntry> AlreadyUnlockedCounts;
		}

		public class UnlockCountEntry
		{
			public Unlock Unlock;
			public int Count;
		}
			
	}
}
