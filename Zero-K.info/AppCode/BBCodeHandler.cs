using System.Web.Mvc;
using System.Web.Routing;

namespace ZeroKWeb
{
	// currently unused
    public static class BBCodeHandler
    {
        // FIXME: do we really need the htmlAttributes?
        public static string Image(this HtmlHelper helper, string url)
        {
            return Image(helper, url, null, null);
        }

        public static string Image(this HtmlHelper helper, string url, string altText)
        {
            return Image(helper, url, altText, null);
        }

        public static string Image(this HtmlHelper helper, string url, string altText, object htmlAttributes)
        {
            // Create tag builder
            var builder = new TagBuilder("img");

            // Add attributes
            builder.MergeAttribute("src", url);
            if(!string.IsNullOrEmpty(altText)) builder.MergeAttribute("alt", altText);
            builder.MergeAttributes(new RouteValueDictionary(htmlAttributes));

            // Render tag
            return builder.ToString(TagRenderMode.SelfClosing);
        }

        public static string Color(this HtmlHelper helper, string color, string inner)
        {
            return Color(helper, color, inner, null);
        }

        public static string Color(this HtmlHelper helper, string color, string inner, object htmlAttributes)
        {
            var builder = new TagBuilder("font");
            builder.MergeAttribute("color", color);
            return builder.ToString(TagRenderMode.StartTag) + inner + builder.ToString(TagRenderMode.EndTag);
        }

        public static string Size(this HtmlHelper helper, string size, string inner)
        {
            return Size(helper, size, inner, null);
        }

        public static string Size(this HtmlHelper helper, string size, string inner, object htmlAttributes)
        {
            var builder = new TagBuilder("font");
            builder.MergeAttribute("size", size);
            return builder.ToString(TagRenderMode.StartTag) + inner + builder.ToString(TagRenderMode.EndTag);
        }

        public static string Url(this HtmlHelper helper, string url, string inner)
        {
            return Url(helper, url, inner, null);
        }

        public static string Url(this HtmlHelper helper, string url, string inner, object htmlAttributes)
        {
            var builder = new TagBuilder("a");
            builder.MergeAttribute("href", url);
            return builder.ToString(TagRenderMode.StartTag) + inner + builder.ToString(TagRenderMode.EndTag);
        }
    }
}