using System.Web.Mvc;
using System.Web.WebPages;

namespace ZeroKWeb
{
    /// <summary>
    /// This class gives access to Html. to @helper pages - like GridHelpers
    /// </summary>
    public class HelperPage : System.Web.WebPages.HelperPage
    {
        // Workaround - exposes the MVC HtmlHelper instead of the normal helper
        public static new HtmlHelper Html
        {
            get { return ((System.Web.Mvc.WebViewPage)WebPageContext.Current.Page).Html; }
        }
    }
}