using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
  public class UsersController: Controller
  {
    //
    // GET: /Users/

    public ActionResult Index(string name)
    {
      var db = new ZkDataContext();
      Account acc;

      int id;
      acc = db.Accounts.FirstOrDefault(x => x.Name == name);
      if (acc == null && int.TryParse(name, out id)) acc = db.Accounts.Single(x => x.AccountID == id);
      
      if (!string.IsNullOrEmpty(name)) return View("UserDetail", acc);
      return View("UserList");
    }
    
    public class UserDetail
    {
      public Account Account;
    }
  }
}