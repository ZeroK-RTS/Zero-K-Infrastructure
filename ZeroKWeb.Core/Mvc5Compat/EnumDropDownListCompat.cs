using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;

namespace System.Web.Mvc
{
    /// <summary>
    /// MVC 5's <c>Html.EnumDropDownListFor</c>, which ASP.NET Core dropped. Fourteen call sites
    /// across seven views: Ladders (3), Battles (7), PlanetwarsAdmin (2), Charts, Factions and
    /// Lobby.
    ///
    /// ASP.NET Core's replacement is <c>DropDownListFor(m =&gt; m.X, Html.GetEnumSelectList&lt;T&gt;())</c>,
    /// which is a different helper emitting different markup, so this reproduces the original
    /// instead - and, as with PostLink, against a capture of the real thing under mono rather
    /// than against a reading of it. See tools/ajax-ground-truth.
    ///
    /// **Four things the capture settled, three of which this would have got wrong:**
    ///
    /// - Options come out in DECLARATION order, not numeric order. <c>Enum.GetNames</c> sorts by
    ///   value, so the obvious implementation silently reorders every dropdown whose members are
    ///   not declared in ascending order. MVC 5 reads the fields, and so does this.
    /// - <c>[Description]</c> is IGNORED. PlanetWarsModes, TreatyUnableToTradeMode, AutohostMode
    ///   and Account.Level all carry it and none carry <c>[Display]</c>, so the live site has
    ///   always shown field names - "AllOffline", not "offline". A port that honoured
    ///   <c>[Description]</c> would look like an improvement and be a regression.
    /// - <c>[Display(Name)]</c> IS honoured.
    /// - An option's value is the NUMBER, not the name, and a nullable enum gets an extra
    ///   <c>&lt;option value=""&gt;&lt;/option&gt;</c> first, which carries the selection when the
    ///   value is null.
    ///
    /// **One thing the capture cannot settle, named rather than hidden:** MVC 5 separates options
    /// with <c>Environment.NewLine</c>, so the live site on Windows emits CRLF and this capture
    /// under mono emits LF. Using the same construct here keeps the two in step on whichever
    /// platform each runs, and the byte comparison in ZeroKWeb.Render is therefore
    /// platform-relative - both sides are Linux there. HTML treats the two identically.
    ///
    /// **A second, also not covered:** MVC 5 prefers ModelState's attempted value over the
    /// model's, which matters only on a redisplayed form whose binding failed. That is
    /// reproduced below on reasoning, not on evidence, because the capture harness has no
    /// ModelState to put a failed binding into.
    /// </summary>
    public static class EnumDropDownListExtensions
    {
        public static MvcHtmlString EnumDropDownListFor<TModel, TEnum>(
            this IHtmlHelper<TModel> html, Expression<Func<TModel, TEnum>> expression)
            => EnumDropDownListFor(html, expression, null);

        public static MvcHtmlString EnumDropDownListFor<TModel, TEnum>(
            this IHtmlHelper<TModel> html, Expression<Func<TModel, TEnum>> expression, object htmlAttributes)
        {
            if (html == null) throw new ArgumentNullException(nameof(html));

            var declared = typeof(TEnum);
            var underlying = Nullable.GetUnderlyingType(declared);
            var enumType = underlying ?? declared;
            if (!enumType.IsEnum)
                throw new ArgumentException("EnumDropDownListFor requires an enum, not " + declared.FullName);

            var name = html.NameFor(expression);
            var selected = SelectedValue(html, expression, name);

            return new MvcHtmlString(
                Render(enumType, underlying != null, html.IdFor(expression), name, selected, htmlAttributes));
        }

        /// <summary>
        /// The markup, given everything already resolved. Split out so that ZeroKWeb.Render can
        /// compare it to the capture without having to build an IHtmlHelper outside a view -
        /// the same arrangement the PostLink check uses. Everything the capture settled lives
        /// here; what stays above is NameFor, IdFor and the ModelState lookup.
        /// </summary>
        internal static string Render(Type enumType, bool isNullable, string id, string name,
                                      string selected, object htmlAttributes)
        {
            var attributes = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (htmlAttributes != null)
                foreach (var attribute in Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes))
                    attributes[attribute.Key] = Convert.ToString(attribute.Value);
            attributes["id"] = id;
            attributes["name"] = name;

            var options = new Text.StringBuilder();
            // A nullable enum gets an empty option first, and it is the one that carries the
            // selection when nothing is chosen.
            if (isNullable) options.Append(Option("", "", selected == null)).Append(Environment.NewLine);

            // GetFields, not Enum.GetNames: declaration order, see the class note.
            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var value = ((Enum)field.GetValue(null)).ToString("d");
                options.Append(Option(value, DisplayName(field), value == selected)).Append(Environment.NewLine);
            }

            return PostLinkExtensions.Tag("select", attributes, options.ToString());
        }

        /// <summary>The numeric string of whatever is currently chosen, or null for none.</summary>
        private static string SelectedValue<TModel, TEnum>(
            IHtmlHelper<TModel> html, Expression<Func<TModel, TEnum>> expression, string name)
        {
            // MVC 5 looks here first - see the class note on what this is and is not based on.
            if (html.ViewData?.ModelState != null
                && html.ViewData.ModelState.TryGetValue(name, out var entry)
                && entry?.AttemptedValue != null)
                return entry.AttemptedValue;

            if (html.ViewData == null || html.ViewData.Model == null) return null;
            var value = expression.Compile()(html.ViewData.Model);
            return value == null ? null : ((Enum)(object)value).ToString("d");
        }

        /// <summary>[Display(Name)] if there is one, otherwise the field's own name.</summary>
        private static string DisplayName(FieldInfo field)
            => field.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? field.Name;

        private static string Option(string value, string text, bool isSelected)
        {
            var attributes = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["value"] = value,
            };
            if (isSelected) attributes["selected"] = "selected";
            return PostLinkExtensions.Tag("option", attributes, WebUtility.HtmlEncode(text ?? ""));
        }
    }
}
