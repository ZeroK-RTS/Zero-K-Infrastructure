using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.UI;
using Microsoft.Linq.Translations;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class LaddersController : Controller
    {
        //
        // GET: /Ladders/
        public ActionResult Index()
        {
            return View("Ladders", Global.LadderCalculator.GetLadder());
        }
    }
}
