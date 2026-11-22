using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace ZeroKWeb
{
    /// <summary>
    /// Rendering a partial view to something a grid cell can hold, behind one name - the same
    /// move as ZkData/DbCompat.cs and Zero-K.info/AppCode/HttpCompat.cs.
    ///
    /// UniGrid formats a cell with <c>AppendFormat("&lt;td&gt;{0}&lt;/td&gt;", value)</c>, which
    /// calls <c>ToString()</c>. On MVC 5 <c>Html.Partial</c> returns an <c>MvcHtmlString</c> whose
    /// ToString IS the html; on ASP.NET Core it returns an <c>IHtmlContent</c> whose ToString is
    /// the type name. The same expression compiles on both and renders on one - which showed up
    /// as a grid full of empty cells and no error at all.
    ///
    /// When the port completes, this file goes and the twin in ZeroKWeb.Core stays.
    /// </summary>
    public static class HtmlCompat
    {
        public static MvcHtmlString PartialString(this HtmlHelper html, string partialViewName, object model)
            => html.Partial(partialViewName, model);
    }
}
