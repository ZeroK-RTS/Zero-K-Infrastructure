using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

// Global namespace for AjaxOptions and InsertionMode, and System.Web.Mvc for the helper, because
// the views spell them exactly as MVC 5 does and Views/Web.config imports System.Web.Mvc.
namespace System.Web.Mvc.Ajax
{
    /// <summary>
    /// MVC 5's <c>AjaxOptions</c>. Same property names, because 18 views set them by name.
    ///
    /// Only the properties this site uses are here. An option MVC 5 supports and Zero-K never
    /// sets is deliberately absent rather than silently ignored: a view that set it would fail
    /// to compile, which is the right outcome, since nothing would have verified its markup.
    /// </summary>
    public class AjaxOptions
    {
        public string Confirm { get; set; }
        public string HttpMethod { get; set; }
        public InsertionMode InsertionMode { get; set; } = InsertionMode.Replace;
        public int LoadingElementDuration { get; set; }
        public string LoadingElementId { get; set; }
        public string OnBegin { get; set; }
        public string OnComplete { get; set; }
        public string OnFailure { get; set; }
        public string OnSuccess { get; set; }
        public string UpdateTargetId { get; set; }
        public string Url { get; set; }
        public bool AllowCache { get; set; }
    }

    /// <summary>MVC 5's insertion modes, with MVC 5's wire spellings.</summary>
    public enum InsertionMode
    {
        Replace = 0,
        InsertBefore = 1,
        InsertAfter = 2,
        ReplaceWith = 3,
    }
}

namespace System.Web.Mvc
{
    using System.Web.Mvc.Ajax;

    /// <summary>
    /// MVC 5's <c>Ajax</c> helper, reimplemented against captured output rather than described
    /// from memory.
    ///
    /// ASP.NET Core removed AjaxHelper. It did NOT remove what the helper produces: MVC 5 with
    /// <c>UnobtrusiveJavaScriptEnabled</c> - which Zero-K.info/Web.config sets to true - emits a
    /// plain tag carrying <c>data-ajax-*</c> attributes, and the script that reads them,
    /// Zero-K.info/Scripts/jquery.unobtrusive-ajax.js, is unchanged and still shipped. So this is
    /// a reimplementation like NoCacheCompat, not a tripwire like ChildActionCompat: the contract
    /// is markup, and the markup is reproducible.
    ///
    /// **The expected markup was captured from MVC 5 itself**, under mono, against the real
    /// System.Web.Mvc 5.2.3 - see tools/ajax-ground-truth/. That mattered twice over:
    ///
    /// - The first capture came back with <c>onsubmit="Sys.Mvc.AsyncForm.handleSubmit(...)"</c>,
    ///   the pre-unobtrusive Microsoft Ajax markup, because the harness had no Web.config and so
    ///   defaulted UnobtrusiveJavaScriptEnabled to false. A capture that looks authoritative and
    ///   describes a different site is worse than no capture.
    /// - The details are not guessable. Attributes come out in ALPHABETICAL order, because MVC
    ///   builds them in a sorted dictionary. UpdateTargetId and LoadingElementId are emitted with
    ///   a '#' prefix; the raw id would select nothing. Apostrophes encode as &amp;#39;, which is
    ///   what the site's own OnComplete strings are full of.
    ///
    /// ZeroKWeb.Render compares this implementation's output against the captured file byte for
    /// byte, with the action URL supplied, since URL generation is MVC's own and not this shim's.
    /// </summary>
    public static class AjaxCompat
    {
        /// <summary>
        /// The tag, given a URL. Separated from the helpers so it can be checked against the
        /// captured MVC 5 output without needing a route table.
        /// </summary>
        public static string BuildTag(string tagName, string url, string urlAttribute, AjaxOptions options,
                                      IDictionary<string, object> htmlAttributes, string innerHtml)
        {
            // Sorted, because MVC 5's TagBuilder holds attributes in a SortedDictionary and the
            // captured output is therefore alphabetical. Rendering them in any other order would
            // be equivalent HTML and a failing byte comparison, which is the point: this file is
            // pinned to observed behaviour, not to what would also have worked.
            var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);

            if (url != null) attributes[urlAttribute] = url;
            foreach (var attribute in AjaxAttributes(options)) attributes[attribute.Key] = attribute.Value;

            if (htmlAttributes != null)
            {
                foreach (var attribute in htmlAttributes)
                    attributes[attribute.Key.Replace('_', '-')] = Convert.ToString(attribute.Value);
            }

            if (tagName == "form") attributes["method"] = options?.HttpMethod?.ToLowerInvariant() ?? "post";

            var text = new StringWriter();
            text.Write("<" + tagName);
            foreach (var attribute in attributes)
                text.Write(" " + attribute.Key + "=\"" + AttributeEncode(attribute.Value) + "\"");
            text.Write(">");
            if (innerHtml != null) text.Write(innerHtml);
            text.Write("</" + tagName + ">");
            return text.ToString();
        }

        /// <summary>
        /// MVC 5's attribute encoding, which ASP.NET Core's <c>HtmlEncoder.Default</c> does not
        /// match. Every rule below was read off the capture, not remembered:
        ///
        ///     &lt;   -&gt; &amp;lt;        &amp;   -&gt; &amp;amp;
        ///     "   -&gt; &amp;quot;      '   -&gt; &amp;#39;      (HtmlEncoder writes &amp;#x27;)
        ///     &gt;   -&gt; &gt;, unescaped               (HtmlEncoder escapes it)
        ///     non-ASCII -&gt; literal UTF-8         (HtmlEncoder escapes it as &amp;#xFC; etc)
        ///
        /// The three disagreements are all harmless in a browser - every form decodes to the same
        /// characters - so this is about byte-identical output between the two stacks, which is
        /// what makes a difference in rendered HTML a signal rather than noise. The site's
        /// OnComplete strings are full of apostrophes, so the third rule alone touches most of
        /// the 18 views.
        /// </summary>
        private static string AttributeEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var text = new System.Text.StringBuilder(value.Length);
            foreach (var character in value)
            {
                switch (character)
                {
                    case '<': text.Append("&lt;"); break;
                    case '&': text.Append("&amp;"); break;
                    case '"': text.Append("&quot;"); break;
                    case '\'': text.Append("&#39;"); break;
                    default: text.Append(character); break;
                }
            }
            return text.ToString();
        }

        /// <summary>The data-ajax-* set, exactly the keys MVC 5 emits for the options it is given.</summary>
        private static IEnumerable<KeyValuePair<string, string>> AjaxAttributes(AjaxOptions options)
        {
            yield return Pair("data-ajax", "true");
            if (options == null) yield break;

            if (!string.IsNullOrEmpty(options.Confirm)) yield return Pair("data-ajax-confirm", options.Confirm);
            if (!string.IsNullOrEmpty(options.OnBegin)) yield return Pair("data-ajax-begin", options.OnBegin);
            if (!string.IsNullOrEmpty(options.OnComplete)) yield return Pair("data-ajax-complete", options.OnComplete);
            if (!string.IsNullOrEmpty(options.OnFailure)) yield return Pair("data-ajax-failure", options.OnFailure);
            if (!string.IsNullOrEmpty(options.OnSuccess)) yield return Pair("data-ajax-success", options.OnSuccess);

            if (!string.IsNullOrEmpty(options.LoadingElementId))
            {
                yield return Pair("data-ajax-loading", "#" + options.LoadingElementId);
                if (options.LoadingElementDuration > 0)
                    yield return Pair("data-ajax-loading-duration", options.LoadingElementDuration.ToString());
            }

            if (!string.IsNullOrEmpty(options.HttpMethod)) yield return Pair("data-ajax-method", options.HttpMethod);
            if (!string.IsNullOrEmpty(options.UpdateTargetId))
            {
                yield return Pair("data-ajax-update", "#" + options.UpdateTargetId);
                yield return Pair("data-ajax-mode", Mode(options.InsertionMode));
            }
            if (!string.IsNullOrEmpty(options.Url)) yield return Pair("data-ajax-url", options.Url);
            if (options.AllowCache) yield return Pair("data-ajax-cache", "true");
        }

        /// <summary>
        /// MVC 5's wire spellings, which jquery.unobtrusive-ajax.js upper-cases and switches on.
        /// "replace" is not one of its cases and falls through to the default, $(update).html(data);
        /// that is how MVC 5 behaves too, and the name is kept rather than corrected.
        /// </summary>
        private static string Mode(InsertionMode mode)
        {
            switch (mode)
            {
                case InsertionMode.InsertBefore: return "before";
                case InsertionMode.InsertAfter: return "after";
                case InsertionMode.ReplaceWith: return "replace-with";
                default: return "replace";
            }
        }

        private static KeyValuePair<string, string> Pair(string key, string value) =>
            new KeyValuePair<string, string>(key, value);

        internal static IDictionary<string, object> ToDictionary(object htmlAttributes) =>
            htmlAttributes == null
                ? null
                // Fully qualified: GlobalUsings.cs aliases HtmlHelper to IHtmlHelper for the views.
                : (IDictionary<string, object>)Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper
                    .AnonymousObjectToHtmlAttributes(htmlAttributes);
    }
}

namespace ZeroKWeb.Compat
{
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;

    /// <summary>
    /// The object the views call as <c>Ajax</c>. Holds the page's url helper and writer, and
    /// defers all markup to <see cref="AjaxCompat.BuildTag"/>, which is the part under test.
    ///
    /// The overload set is the one the 18 views actually use. Anything else fails to compile,
    /// on purpose - see the note in AjaxCompat.
    /// </summary>
    public class Mvc5AjaxHelper
    {
        private readonly Microsoft.AspNetCore.Mvc.IUrlHelper url;
        private readonly TextWriter writer;

        public Mvc5AjaxHelper(Microsoft.AspNetCore.Mvc.IUrlHelper url, TextWriter writer)
        {
            this.url = url;
            this.writer = writer;
        }

        public IDisposable BeginForm(AjaxOptions options) => Form(null, null, null, options, null);

        public IDisposable BeginForm(string action, AjaxOptions options) => Form(action, null, null, options, null);

        public IDisposable BeginForm(string action, object routeValues, AjaxOptions options)
            => Form(action, null, routeValues, options, null);

        public IDisposable BeginForm(string action, string controller, AjaxOptions options)
            => Form(action, controller, null, options, null);

        public IDisposable BeginForm(string action, string controller, object routeValues, AjaxOptions options)
            => Form(action, controller, routeValues, options, null);

        public IDisposable BeginForm(string action, string controller, object routeValues, AjaxOptions options,
                                     object htmlAttributes)
            => Form(action, controller, routeValues, options, htmlAttributes);

        public IHtmlContent ActionLink(string text, string action, object routeValues, AjaxOptions options)
            => new HtmlString(AjaxCompat.BuildTag("a", Url(action, null, routeValues), "href", options,
                                                  null, HtmlEncoder.Default.Encode(text ?? "")));


        private IDisposable Form(string action, string controller, object routeValues, AjaxOptions options,
                                 object htmlAttributes)
        {
            var tag = AjaxCompat.BuildTag("form", Url(action, controller, routeValues), "action", options,
                                          AjaxCompat.ToDictionary(htmlAttributes), "");
            // BuildTag returns a closed element; a form written with @using needs its two halves
            // separately, and splitting the rendered string keeps one implementation of the
            // markup rather than two that can drift.
            writer.Write(tag.Substring(0, tag.Length - "</form>".Length));
            return new EndTag(writer, "</form>");
        }

        private string Url(string action, string controller, object routeValues)
        {
            if (action == null && controller == null && routeValues == null) return url.Action();
            if (controller == null && routeValues == null) return url.Action(action);
            if (controller == null) return url.Action(action, routeValues);
            if (routeValues == null) return url.Action(action, controller);
            return url.Action(action, controller, routeValues);
        }

        private sealed class EndTag : IDisposable
        {
            private readonly TextWriter writer;
            private readonly string text;
            public EndTag(TextWriter writer, string text) { this.writer = writer; this.text = text; }
            public void Dispose() => writer.Write(text);
        }
    }
}
