using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
  public class MyController: Controller
  {
    //
    // GET: /My/

    public ActionResult Commanders()
    {
      if (!Global.IsAccountAuthorized) return Content("Not logged in");
      var db = new ZkDataContext();

      var ret = new CommandersModel();
      ret.Commanders = db.Commanders.Where(x => x.AccountID == Global.AccountID).ToList();
      ret.Unlocks =
        Global.Account.AccountUnlocks.Select(x => x.Unlock).Where(
          x => x.UnlockType == UnlockTypes.Module || x.UnlockType == UnlockTypes.Weapon || x.UnlockType == UnlockTypes.Chassis).ToList();
      ret.Slots = db.CommanderSlots.ToList();
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

    public ActionResult Unlock(int id)
    {
      if (!Global.IsAccountAuthorized) return Content("Not logged in");
      var db = new ZkDataContext();

      List<Unlock> unlocks;
      List<Unlock> future;

      GetUnlockLists(out unlocks, out future);

      if (Global.Account.Level > Global.Account.AccountUnlocks.Count && unlocks.Any(x => x.UnlockID == id))
      {
        if (!db.AccountUnlocks.Any(x => x.AccountID == Global.AccountID && x.UnlockID == id))
        {
          var entry = new AccountUnlock() { AccountID = Global.AccountID, UnlockID = id };
          db.AccountUnlocks.InsertOnSubmit(entry);
          db.SubmitChanges();
        }
      }
      return RedirectToAction("UnlockList");
    }

    public ActionResult UnlockList()
    {
      if (!Global.IsAccountAuthorized) return Content("Not logged in");
      List<Unlock> unlocks;
      List<Unlock> future;
      GetUnlockLists(out unlocks, out future);

      return View("UnlockList", new UnlockListResult() { Account = Global.Account, Unlocks = unlocks, FutureUnlocks = future });
    }

    void GetUnlockLists(out List<Unlock> unlocks, out List<Unlock> future)
    {
      var db = new ZkDataContext();

      var myUnlockList = Global.Account.AccountUnlocks.Select(x => x.UnlockID).ToList();

      var temp =
        db.Unlocks.Where(
          x =>
          x.NeededLevel <= Global.Account.Level && !myUnlockList.Contains(x.UnlockID) &&
          (x.RequiredUnlockID == null || myUnlockList.Contains(x.RequiredUnlockID ?? 0))).OrderBy(x => x.NeededLevel).ThenBy(x => x.UnlockType).ToList
          ();
      unlocks = temp;

      future =
        db.Unlocks.Where(x => !myUnlockList.Contains(x.UnlockID) && !temp.Select(y => y.UnlockID).Contains(x.UnlockID)).OrderBy(x => x.NeededLevel).
          ToList();
    }

    public ActionResult SubmitCommander(int? commanderID, int profileNumber, string name, int? chassis)
    {
      if (!Global.IsAccountAuthorized) return Content("Not logged in");
      if (profileNumber <1 || profileNumber > 4) return Content("WTF! get lost");

      var db = new ZkDataContext();

      List<Unlock> unlocks = Global.Account.AccountUnlocks.Select(x => x.Unlock).ToList();

      Commander comm;
      if (commanderID != null)
      {
        comm = db.Commanders.Single(x => x.CommanderID == commanderID && x.AccountID == Global.AccountID);
      } else
      {
        comm = new Commander() { AccountID = Global.AccountID, ProfileNumber = profileNumber };
        db.Commanders.InsertOnSubmit(comm);
      }
      comm.Name = name;
      

      if (chassis == null || !unlocks.Any(x=>x.UnlockID == chassis.Value)) return Content("Invalid chassis");
      comm.ChassisUnlockID = chassis.Value;

      foreach (var key in Request.Form.AllKeys)
      {
        var m = Regex.Match(key, "m([0-9]+)");
        if (m.Success)
        {
          int slotId = int.Parse(m.Groups[1].Value);
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
              if (!validChassis.Contains(comm.Unlock.Name)) return Content(string.Format("{0} cannot be used in commander {1}", unlock.Name, comm.Unlock.Name));
            }

            var comSlot = comm.CommanderModules.SingleOrDefault(x => x.SlotID == slotId);
            if (comSlot == null)
            {
              comSlot = new CommanderModule() { SlotID = slotId };
              comm.CommanderModules.Add(comSlot);
            }
            comSlot.ModuleUnlockID = unlockId;
          }
        }
      }
      db.SubmitChanges();


      return RedirectToAction("Commanders");

    }


    public class CommandersModel
    {
      public List<Commander> Commanders;
      public List<CommanderSlot> Slots;
      public List<Unlock> Unlocks;
    }


    public class UnlockListResult
    {
      public Account Account;
      public IEnumerable<Unlock> FutureUnlocks;
      public IEnumerable<Unlock> Unlocks;
    }
  }
}