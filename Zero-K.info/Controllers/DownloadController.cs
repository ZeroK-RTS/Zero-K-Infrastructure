using PlasmaShared;
using Ratings;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Helpers;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{

    public class DownloadController : Controller
    {

        // GET: Download
        public ActionResult Index()
        {
            return View("DownloadIndex");
        }
    }
}