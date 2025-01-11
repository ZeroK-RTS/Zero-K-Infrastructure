using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            try
            {
                var url = ReplayStorage.Instance.GetFileUrl(name);
                if (url != null) return Redirect(url);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error downloading replay {0}, attempting local copy: {1}", name, ex.Message);
            }

            return File(ReplayStorage.Instance.GetLocalFileContent(name), "application/octet-stream", name);
        }

    }
}
