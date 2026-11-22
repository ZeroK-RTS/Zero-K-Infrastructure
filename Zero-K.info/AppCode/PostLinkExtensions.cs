using System.Web.Mvc.Html;
using System.Web.Routing;

namespace System.Web.Mvc
{
    /// <summary>
    /// Renders a link-looking control that submits via POST and carries an anti-forgery token.
    /// Used for actions that change state: a plain &lt;a&gt; leaves them reachable by GET, which means
    /// any third-party page can trigger them with the visitor's cookies attached.
    /// Pass cssClass "js_confirm" to reuse the site's existing confirmation dialog, or
    /// confirmMessage to ask a specific question instead of the generic one.
    /// </summary>
    public static class PostLinkExtensions
    {
        /// <summary>Text link that POSTs to an action.</summary>
        public static MvcHtmlString PostLink(this HtmlHelper html,
                                             string linkText,
                                             string action,
                                             string controller = null,
                                             object routeValues = null,
                                             string cssClass = null,
                                             string nicetitle = null,
                                             string confirmMessage = null) {
            return BuildPostForm(html, HttpUtility.HtmlEncode(linkText ?? ""), action, controller, routeValues, cssClass, nicetitle, confirmMessage);
        }

        /// <summary>Image link that POSTs to an action. imageHeight of 0 omits the attribute.</summary>
        public static MvcHtmlString PostImageLink(this HtmlHelper html,
                                                  string imageSrc,
                                                  int imageHeight,
                                                  string action,
                                                  string controller = null,
                                                  object routeValues = null,
                                                  string cssClass = null,
                                                  string nicetitle = null,
                                                  string confirmMessage = null) {
            var img = new TagBuilder("img");
            img.Attributes["src"] = imageSrc;
            if (imageHeight > 0) img.Attributes["height"] = imageHeight.ToString();
            img.Attributes["alt"] = "";
            return BuildPostForm(html, img.ToString(TagRenderMode.SelfClosing), action, controller, routeValues, cssClass, nicetitle, confirmMessage);
        }

        static MvcHtmlString BuildPostForm(HtmlHelper html,
                                           string innerHtml,
                                           string action,
                                           string controller,
                                           object routeValues,
                                           string cssClass,
                                           string nicetitle,
                                           string confirmMessage) {
            var urlHelper = new UrlHelper(html.ViewContext.RequestContext);
            var values = routeValues == null ? new RouteValueDictionary() : new RouteValueDictionary(routeValues);
            var url = controller == null ? urlHelper.Action(action, values) : urlHelper.Action(action, controller, values);

            var form = new TagBuilder("form");
            form.Attributes["method"] = "post";
            form.Attributes["action"] = url;
            form.AddCssClass("postlink");

            var button = new TagBuilder("button");
            button.Attributes["type"] = "submit";
            // site_main.js skips .postlink-button when it buttonifies :submit with jQuery UI
            button.AddCssClass("postlink-button");
            if (!string.IsNullOrEmpty(cssClass)) button.AddCssClass(cssClass);
            if (!string.IsNullOrEmpty(nicetitle)) button.Attributes["nicetitle"] = nicetitle;
            // An ATTRIBUTE, not an onclick. The call sites this replaces built
            //     onclick="return confirm('...{0}...')"
            // with String.Format, so a quote in the interpolated value - a planet name, which
            // only SubmitRenamePlanet's moderator check constrains - ended the JavaScript string
            // early. Here the text is attribute-encoded like any other, and site_main.js reads it
            // back with jQuery, so there is no string to escape into.
            if (!string.IsNullOrEmpty(confirmMessage)) button.Attributes["data-confirm"] = confirmMessage;
            button.InnerHtml = innerHtml;

            form.InnerHtml = html.AntiForgeryToken().ToHtmlString() + button.ToString(TagRenderMode.Normal);
            return new MvcHtmlString(form.ToString(TagRenderMode.Normal));
        }
    }
}
