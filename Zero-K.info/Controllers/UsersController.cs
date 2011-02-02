using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class UsersController : Controller
    {
        //
        // GET: /Users/

        public ActionResult Index(string name)
        {
          var db = new ZkDataContext();
          Account acc;

          int id;
          if (int.TryParse(name, out id)) acc = db.Accounts.Single(x => x.AccountID == id);
          else acc = db.Accounts.First(x => x.Name == name);

          if (!string.IsNullOrEmpty(name))
          {
            return View("UserDetail", acc);


          }
          return View();
        }

        public class UserDetail
        {
          public Account Account;
        }

    }
}
