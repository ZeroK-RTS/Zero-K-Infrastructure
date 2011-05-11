using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class MyController: Controller
	{
		//
		// GET: /My/
		[Auth]
		public ActionResult CommanderProfile(int profileNumber, string name, int? chassis, string deleteCommander)
		{
			if (profileNumber < 1 || profileNumber > 4) return Content("WTF! get lost");

			var db = new ZkDataContext();
			using (var scope = new TransactionScope())
			{

				var unlocks = db.AccountUnlocks.Where(x => x.AccountID == Global.AccountID);

				var comm = db.Commanders.SingleOrDefault(x => x.ProfileNumber == profileNumber && x.AccountID == Global.AccountID);
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

				if (!string.IsNullOrEmpty(name)) comm.Name = name;

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
							var slot = db.CommanderSlots.Single(x => x.CommanderSlotID == slotId);
							var unlock = db.Unlocks.Single(x => x.UnlockID == unlockId);

							if (!unlocks.Any(x => x.UnlockID == unlock.UnlockID)) return Content("WTF get lost!");
							if (slot.MorphLevel < unlock.MorphLevel) return Content(string.Format("WTF cannot use {0} in slot {1}", unlock.Name, slot.CommanderSlotID));
							if (!string.IsNullOrEmpty(unlock.LimitForChassis))
							{
								var validChassis = unlock.LimitForChassis.Split(',');
								if (!validChassis.Contains(comm.Unlock.Code)) return Content(string.Format("{0} cannot be used in commander {1}", unlock.Name, comm.Unlock.Name));
							}

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

				foreach (var toDel in Request.Form.AllKeys.Where(x => !string.IsNullOrEmpty(x)))
				{
					var m = Regex.Match(toDel, "deleteSlot([0-9]+)");
					if (m.Success)
					{
						var slotId = int.Parse(m.Groups[1].Value);
						comm.CommanderModules.Remove(comm.CommanderModules.SingleOrDefault(x => x.SlotID == slotId));
					}
				}

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

		[Auth]
		public ActionResult Commanders()
		{
			var db = new ZkDataContext();

			var ret = new CommandersModel();
			ret.Unlocks =
				Global.Account.AccountUnlocks.Where(
					x => x.Unlock.UnlockType == UnlockTypes.Module || x.Unlock.UnlockType == UnlockTypes.Weapon || x.Unlock.UnlockType == UnlockTypes.Chassis).ToList
					();
			return View(ret);
		}

		public ActionResult Index()
		{
			return RedirectToAction("Index", "Users", new { name = Global.Account.Name });
		}


		public ActionResult Reset()
		{
			var db = new ZkDataContext();
			db.AccountUnlocks.DeleteAllOnSubmit(db.AccountUnlocks.Where(x => x.AccountID == Global.AccountID));
			db.SubmitChanges();
			return RedirectToAction("UnlockList");
		}

		[Auth]
		public ActionResult Unlock(int id)
		{
			using (var db = new ZkDataContext())
			using (var scope = new TransactionScope())
			{

				List<Unlock> unlocks;
				List<Unlock> future;

				GetUnlockLists(db, out unlocks, out future);

				if (unlocks.Any(x => x.UnlockID == id))
				{
					var au = db.AccountUnlocks.SingleOrDefault(x => x.AccountID == Global.AccountID && x.UnlockID == id);
					if (au == null)
					{
						au = new AccountUnlock() { AccountID = Global.AccountID, UnlockID = id, Count = 1 };
						db.AccountUnlocks.InsertOnSubmit(au);
					}
					else au.Count++;
					db.SubmitChanges();
				}
				scope.Complete();
			}
			return RedirectToAction("UnlockList");
		}

		[Auth]
		public ActionResult UnlockList()
		{
			List<Unlock> unlocks;
			List<Unlock> future;
			var db = new ZkDataContext();
			GetUnlockLists(db, out unlocks, out future);

			return View("UnlockList", new UnlockListResult() { Account = Global.Account, Unlocks = unlocks, FutureUnlocks = future });
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
			                   	Unlocks =
			                   		db.AccountUnlocks.Where(
			                   			x =>
			                   			x.AccountID == Global.AccountID &&
			                   			(x.Unlock.UnlockType == UnlockTypes.Module || x.Unlock.UnlockType == UnlockTypes.Weapon ||
			                   			 x.Unlock.UnlockType == UnlockTypes.Chassis)).ToList().Where(
			                   			 	x =>
			                   			 	(com == null || x.Unlock.LimitForChassis == null || x.Unlock.LimitForChassis.Contains(com.Unlock.Code)) &&
			                   			 	(com == null || x.Count > com.CommanderModules.Count(y => y.ModuleUnlockID == x.UnlockID))).Select(x => x.Unlock).ToList()
			                   });
		}

		public static void GetUnlockLists(ZkDataContext db, out List<Unlock> unlocks, out List<Unlock> future)
		{
			var maxedUnlockList =
				db.AccountUnlocks.Where(x => x.AccountID == Global.AccountID && x.Count >= x.Unlock.MaxModuleCount).Select(x => x.UnlockID).ToList();
			var anyUnlockList = db.AccountUnlocks.Where(x => x.AccountID == Global.AccountID && x.Count > 0).Select(x => x.UnlockID).ToList();

			var temp =
				db.Unlocks.Where(
					x =>
					x.NeededLevel <= Global.Account.Level && x.XpCost <= Global.Account.AvailableXP && !maxedUnlockList.Contains(x.UnlockID) &&
					(x.RequiredUnlockID == null || anyUnlockList.Contains(x.RequiredUnlockID ?? 0))).OrderBy(x => x.NeededLevel).ThenBy(x => x.XpCost).ThenBy(
						x => x.UnlockType).ToList();
			unlocks = temp;

			future =
				db.Unlocks.Where(x => !maxedUnlockList.Contains(x.UnlockID) && !temp.Select(y => y.UnlockID).Contains(x.UnlockID)).OrderBy(x => x.NeededLevel).
					ThenBy(x => x.XpCost).ToList();
		}


		public class CommanderProfileModel
		{
			public Commander Commander;
			public int ProfileID;
			public List<CommanderSlot> Slots;
			public List<Unlock> Unlocks;
		}

		public class CommandersModel
		{
			public List<AccountUnlock> Unlocks;
		}


		public class UnlockListResult
		{
			public Account Account;
			public IEnumerable<Unlock> FutureUnlocks;
			public IEnumerable<Unlock> Unlocks;
		}
	}
}