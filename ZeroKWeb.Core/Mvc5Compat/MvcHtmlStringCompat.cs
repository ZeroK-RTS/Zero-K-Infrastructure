namespace System.Web.Mvc
{
    /// <summary>
    /// MVC 5's <c>MvcHtmlString</c>: a string already known to be safe HTML.
    ///
    /// ASP.NET Core's equivalent is <c>HtmlString</c>, and the two differ only in name - both
    /// wrap a string and both say "do not encode this again". So this is a subclass rather than
    /// a conversion, which means a linked file can keep returning <c>MvcHtmlString</c> while
    /// every ASP.NET Core API that wants an <c>IHtmlContent</c> accepts the same object.
    ///
    /// <c>Create</c> exists because MVC 5 has it and call sites use it; MVC 5 returns null for a
    /// null input and that is kept.
    /// </summary>
    public class MvcHtmlString : Microsoft.AspNetCore.Html.HtmlString
    {
        public static readonly MvcHtmlString Empty = new MvcHtmlString("");

        public MvcHtmlString(string value) : base(value) { }

        public static MvcHtmlString Create(string value) => value == null ? null : new MvcHtmlString(value);

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        /// <summary>
        /// MVC 5's IHtmlString.ToHtmlString. ASP.NET Core's IHtmlContent writes itself to a
        /// writer instead, and HtmlString.Value is already the raw markup - these are strings
        /// that were built as html, so there is nothing to encode on the way out.
        /// </summary>
        public string ToHtmlString() => Value;
    }
}

namespace System.Web.WebPages
{
    /// <summary>
    /// A namespace, not a type. <c>AppCode/UniGrid/Col.cs</c> writes
    /// <c>using System.Web.WebPages;</c> for <c>HelperResult</c>, and a using of a namespace no
    /// type declares is CS0246 - so something has to declare it. The name it wants,
    /// <c>HelperResult</c>, is supplied as a global using alias onto ASP.NET Core's own, because
    /// a SUBCLASS here would not accept what a templated Razor delegate produces.
    ///
    /// This is the same shape as the System.Web.Mvc namespace next door: a real namespace that
    /// is deliberately almost empty, so linked files compile unedited.
    /// </summary>
    internal static class NamespaceMarker { }
}
