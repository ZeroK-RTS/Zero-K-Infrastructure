using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class LobbyServiceController : Controller
    {
        // GET: LobbyServer
        public JsonResult Login(string login, string password)
        {
            var db = new ZkDataContext();
            var acc = Account.AccountVerify(db, login, password);
            return new JsonResult() {Data = new LoginResponse() {Ok = acc != null, Reason = "test", Account = acc}, JsonRequestBehavior = JsonRequestBehavior.AllowGet};
        }

        public class LoginResponse
        {
            public bool Ok;
            public string Reason;
            public int 
            public Account Account;
        }

        public JsonResult Register(string login, string password)
        {
            var db = new ZkDataContext();
            var acc = Account.AccountVerify(db, login, password);
            return new JsonResult() { Data = new LoginResponse() { Ok = acc != null, Reason = "test", Account = acc }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public class RegisterResponse
        {
            public bool Ok;
            public string Reason;
            public Account Account;
        }

    }
}