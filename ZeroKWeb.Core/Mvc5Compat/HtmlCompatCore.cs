using System.IO;
using System.Text.Encodings.Web;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ZeroKWeb
{
    /// <summary>
    /// The ASP.NET Core twin of Zero-K.info/AppCode/HtmlCompat.cs. Same method name, so the
    /// linked call site compiles against either framework.
    ///
    /// The difference it exists for: ASP.NET Core's Html.Partial returns an IHtmlContent, which
    /// has to be WRITTEN rather than converted - its ToString is the type name. Anything that
    /// puts a partial's output into a string, as UniGrid's cell formatting does, needs this.
    /// </summary>
    public static class HtmlCompat
    {
        public static MvcHtmlString PartialString(this IHtmlHelper html, string partialViewName, object model)
        {
            var content = html.Partial(partialViewName, model);
            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, HtmlEncoder.Default);
                return new MvcHtmlString(writer.ToString());
            }
        }
    }
}
