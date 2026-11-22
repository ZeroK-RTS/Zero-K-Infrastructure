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

            // Neither blob storage nor local disk has it. GetLocalFileContent returns null for
            // "not there", and File(null, ...) throws on both stacks - so asking for a replay that
            // does not exist used to be a 500.
            //
            // 404 rather than the Content("No such ...") this codebase usually writes, because
            // this endpoint returns a FILE: a browser handed 200 and a sentence saves the sentence
            // as a .sdfz. The lobby client cannot tell the two apart either way - PlasmaDownloader
            // fetches this with WebClient, which raises e.Error for any non-success status - so
            // nothing downstream changes behaviour.
            //
            // What does change is on this side. Since the site's trace listener was reinstalled,
            // every unhandled exception writes a LogEntries row, so a missing replay was about to
            // start filling Admin/TraceLogs with a failure that is an ordinary 404.
            var content = ReplayStorage.Instance.GetLocalFileContent(name);
            if (content == null) return this.HttpNotFound();

            return File(content, "application/octet-stream", name);
        }

    }
}
