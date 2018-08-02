using System.Web.Mvc;

namespace ZeroKWeb.Controllers
{
    public class StaticController: Controller
    {
        //
        // GET: /Static/
        public ActionResult Index(string name = "LobbyStart") {
            if (name == "UnitGuide") return View("Index", (object)"/mediawiki/index.php?title=Units");
            return Content("");
        }
    }
}