using System.Net;
using System.Web.Mvc;

namespace ZeroKWeb.Controllers
{
  public class WikiController: Controller
  {
    //
    // GET: /Wiki/
    public ActionResult Index(string node)
    {
      string ret = WikiHandler.LoadWiki(node);

      return View(new WikiData() { Content = new MvcHtmlString(ret) });
    }

    public class WikiData
    {
      public MvcHtmlString Content;
    }
  }
}