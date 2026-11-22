using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace System.Web.Mvc
{
    /// <summary>
    /// The ASP.NET Core twin of Zero-K.info/AppCode/PostLinkExtensions.cs.
    ///
    /// Not a relocation. MVC 5's <c>TagBuilder</c> and ASP.NET Core's share a name and little
    /// else: <c>InnerHtml</c> is a string on one and an <c>IHtmlContentBuilder</c> on the other,
    /// <c>ToString(TagRenderMode)</c> does not exist here at all, and <c>UrlHelper</c> is built
    /// from a <c>RequestContext</c> there and from an <c>ActionContext</c> here. So the markup is
    /// produced directly, and matched against a capture of the original rather than against
    /// anyone's reading of it - see tools/ajax-ground-truth, and ZeroKWeb.Render, which compares
    /// all six shapes byte for byte.
    ///
    /// Three details the capture settled, none of which are guessable:
    ///
    /// - Attributes come out in ALPHABETICAL order, because MVC 5's TagBuilder holds them in a
    ///   sorted dictionary.
    /// - <c>AddCssClass</c> PREPENDS, so a cssClass argument lands before "postlink-button"
    ///   rather than after it.
    /// - The text is encoded with HttpUtility.HtmlEncode, which spells an apostrophe
    ///   <c>&amp;#39;</c> and leaves <c>&gt;</c> alone - not ASP.NET Core's HtmlEncoder.
    ///
    /// **The anti-forgery token is the one thing the capture could not cover.** MVC 5's
    /// anti-forgery reads web.config, which mono cannot host, and the token is random per request
    /// in any case. It is emitted here through ASP.NET Core's own antiforgery service, and
    /// ZeroKWeb.Render asserts it is PRESENT - a port that dropped it would turn every one of
    /// these links into a CSRF hole and still match the captured structure exactly.
    /// </summary>
    public static class PostLinkExtensions
    {
        public static MvcHtmlString PostLink(this IHtmlHelper html,
                                             string linkText,
                                             string action,
                                             string controller = null,
                                             object routeValues = null,
                                             string cssClass = null,
                                             string nicetitle = null,
                                             string confirmMessage = null)
            => BuildPostForm(html, WebUtility.HtmlEncode(linkText ?? ""),
                             action, controller, routeValues, cssClass, nicetitle, confirmMessage);

        public static MvcHtmlString PostImageLink(this IHtmlHelper html,
                                                  string imageSrc,
                                                  int imageHeight,
                                                  string action,
                                                  string controller = null,
                                                  object routeValues = null,
                                                  string cssClass = null,
                                                  string nicetitle = null,
                                                  string confirmMessage = null)
        {
            var img = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["alt"] = "",
                ["src"] = imageSrc,
            };
            if (imageHeight > 0) img["height"] = imageHeight.ToString();
            return BuildPostForm(html, Tag("img", img, null, selfClosing: true),
                                 action, controller, routeValues, cssClass, nicetitle, confirmMessage);
        }

        private static MvcHtmlString BuildPostForm(IHtmlHelper html,
                                                   string innerHtml,
                                                   string action,
                                                   string controller,
                                                   object routeValues,
                                                   string cssClass,
                                                   string nicetitle,
                                                   string confirmMessage)
        {
            var url = Url(html, action, controller, routeValues) ?? "";

            var buttonClass = string.IsNullOrEmpty(cssClass)
                ? "postlink-button"
                // AddCssClass prepends, which the capture is what established.
                : cssClass + " postlink-button";

            var button = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["type"] = "submit",
                ["class"] = buttonClass,
            };
            if (!string.IsNullOrEmpty(nicetitle)) button["nicetitle"] = nicetitle;
            if (!string.IsNullOrEmpty(confirmMessage)) button["data-confirm"] = confirmMessage;

            var form = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["method"] = "post",
                ["action"] = url,
                ["class"] = "postlink",
            };

            return new MvcHtmlString(Tag("form", form,
                AntiForgeryToken(html) + Tag("button", button, innerHtml), selfClosing: false));
        }

        /// <summary>
        /// ASP.NET Core's own antiforgery token, in the shape MVC 5 emitted: a hidden input named
        /// __RequestVerificationToken. Empty when no antiforgery service is registered, which is
        /// the case in the render harness - hence the check there asserts the input is present
        /// when it is hosted, not here.
        /// </summary>
        private static string AntiForgeryToken(IHtmlHelper html)
        {
            var content = html?.AntiForgeryToken();
            if (content == null) return "";
            using (var writer = new IO.StringWriter())
            {
                content.WriteTo(writer, Text.Encodings.Web.HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        private static string Url(IHtmlHelper html, string action, string controller, object routeValues)
        {
            var url = ZeroKWeb.Global.UrlHelper();
            if (url == null) return null;
            var values = routeValues == null ? new RouteValueDictionary() : new RouteValueDictionary(routeValues);
            return controller == null ? url.Action(action, values) : url.Action(action, controller, values);
        }

        /// <summary>
        /// Renders an element the way MVC 5's TagBuilder does: attributes in the order the sorted
        /// dictionary yields them, values HtmlAttributeEncoded, self-closing written " /&gt;".
        /// </summary>
        internal static string Tag(string name, SortedDictionary<string, string> attributes,
                                   string innerHtml, bool selfClosing = false)
        {
            var text = new Text.StringBuilder("<").Append(name);
            foreach (var attribute in attributes)
                text.Append(' ').Append(attribute.Key).Append("=\"")
                    .Append(AttributeEncode(attribute.Value)).Append('"');
            if (selfClosing) return text.Append(" />").ToString();
            return text.Append('>').Append(innerHtml).Append("</").Append(name).Append('>').ToString();
        }

        /// <summary>
        /// HttpUtility.HtmlAttributeEncode's rules, which AjaxCompat also reproduces and for the
        /// same reason: ASP.NET Core's HtmlEncoder disagrees on the apostrophe, on '&gt;' and on
        /// non-ASCII. See the table in AjaxCompat.
        /// </summary>
        private static string AttributeEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var text = new Text.StringBuilder(value.Length);
            foreach (var character in value)
                switch (character)
                {
                    case '<': text.Append("&lt;"); break;
                    case '&': text.Append("&amp;"); break;
                    case '"': text.Append("&quot;"); break;
                    case '\'': text.Append("&#39;"); break;
                    default: text.Append(character); break;
                }
            return text.ToString();
        }
    }
}
