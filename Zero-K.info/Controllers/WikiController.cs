using System.Net;
using System.Web.Mvc;

namespace ZeroKWeb.Controllers
{
  public class WikiController: Controller
  {
    //
    // GET: /Wiki/
    public ActionResult Index(string node, bool minimal = false)
    {
      string ret = WikiHandler.LoadWiki(node);

      if (minimal) return Content(ret);
      else return View(new WikiData() { Content = new MvcHtmlString(ret), Node = node});
    }

    public class WikiData
    {
    	public string Node;
      public MvcHtmlString Content;
    }
  }
}