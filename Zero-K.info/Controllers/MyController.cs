using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
  public class MyController: Controller
  {
    //
    // GET: /My/

    public ActionResult Index()
    {
      return RedirectToAction("Index", "Users", new { name = Global.Account.Name });
    }


    public ActionResult Reset()
    {
      var db = new ZkDataContext();
      db.AccountUnlocks.DeleteAllOnSubmit(db.AccountUnlocks.Where(x=>x.AccountID == Global.AccountID));
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

    public class UnlockListResult
    {
      public Account Account;
      public IEnumerable<Unlock> FutureUnlocks;
      public IEnumerable<Unlock> Unlocks;
    }
  }
}