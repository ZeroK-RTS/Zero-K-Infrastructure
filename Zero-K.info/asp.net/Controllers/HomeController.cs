using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/


			public ActionResult Logon(string login, string password, string referer)
			{
				var db = new ZkDataContext();
				
				var acc = db.Accounts.SingleOrDefault(x => x.Name == login);
				if (acc == null) return Content("Invalid login name");
				var hashed = Utils.HashLobbyPassword(password);
				acc = AuthServiceClient.VerifyAccountHashed(login, hashed);
				if (acc == null) return Content("Invalid password");
				else
				{
					Response.SetCookie(new HttpCookie(GlobalConst.LoginCookieName, login));
					Response.SetCookie(new HttpCookie(GlobalConst.PasswordHashCookieName, hashed));
					
					return Redirect(referer);
				}
			}

    }
}
