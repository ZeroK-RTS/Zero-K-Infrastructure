using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ZeroKWeb.Controllers
{
    public class ReplaysController : Controller
    {
        //
        // GET: /Replays/

        public ActionResult Index() {
            return Content("");
        }
        
        string[] possiblePaths = new string[] { @"c:\springie_spring\demos-server", @"c:\springie_spring\demos" };
    
        public ActionResult Download(string name) {
            if (string.IsNullOrEmpty(name)) {
                return Content("");
            }
            else {
                foreach (var p in possiblePaths) {
                    var path = Path.Combine(p, name);
                    if (System.IO.File.Exists(path)) return File(System.IO.File.OpenRead(path), "application/octet-stream");
                }
            }
            throw new HttpException(404,"Demo file not found");
        }

    }
}
