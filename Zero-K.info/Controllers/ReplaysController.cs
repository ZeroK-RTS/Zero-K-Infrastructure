using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ZkData;
using ZkLobbyServer;

namespace ZeroKWeb.Controllers
{
    public class ReplaysController : Controller
    {
        public ActionResult Index() {
            return Content("");
        }
        
        public ActionResult Download(string name)
        {
            return Redirect(ReplayStorage.Instance.GetFileUrl(name));
        }

    }
}
