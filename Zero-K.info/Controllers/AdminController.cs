using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class AdminController : Controller
    {
        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult ResetDb() {
            if (GlobalConst.Mode == ModeType.Test)
            {
                var cloner = new DbCloner("zero-k", "zero-k_test", GlobalConst.ZkDataContextConnectionString);
                cloner.CloneAllTables();
                return Content("DONE! Database copied");
            } else return Content("Not allowed!");
        }
    }
}