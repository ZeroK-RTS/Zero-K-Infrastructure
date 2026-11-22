using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using PlasmaShared;

namespace System.Web.Mvc
{
    /// <summary>Copied from HtmlHelperExtensions.cs, where it sits beside the helpers below.</summary>
    public class SelectOption
    {
        public string Name;
        public string Value;
    }

    /// <summary>
    /// Three of the site's own helpers from Zero-K.info/AppCode/HtmlHelperExtensions.cs, which
    /// cannot be linked: they take <c>HtmlHelper&lt;TModel&gt;</c> and call
    /// <c>ModelMetadata.FromLambdaExpression</c> and <c>ExpressionHelper.GetExpressionText</c>,
    /// none of which ASP.NET Core has in that shape.
    ///
    /// Between them they block four views - Admin/TraceLogs, Battles/BattleIndex,
    /// Charts/ChartsRatings, Tourney/TourneyIndex - plus Missions/Detail for Select.
    ///
    /// **The format strings below are copied character for character from the original.** That
    /// is the whole of the markup, so a diff against HtmlHelperExtensions.cs is the check for
    /// it; there is nothing hidden behind a TagBuilder here, which is what made PostLink and
    /// EnumDropDownListFor need a capture and what makes these not need one.
    ///
    /// What DID need capturing is the pair of MVC 5 APIs underneath, because the port reaches
    /// them differently and the substitutes are not obviously equal. The capture records:
    ///
    ///     name=UserId model=[4,11]
    ///
    /// so <c>ExpressionHelper.GetExpressionText</c> gives the bare property name - which
    /// <c>NameFor</c> also gives, because it is that text with the field prefix in front and the
    /// prefix is empty at the top level, where all five call sites are - and
    /// <c>FromLambdaExpression(...).Model</c> gives the live list, which a compiled expression
    /// also gives.
    ///
    /// Encoding is <see cref="WebUtility"/>, not <c>IHtmlHelper.Encode</c>: MVC 5 encoded with
    /// HttpUtility's rules, which spell an apostrophe <c>&amp;#39;</c>, and ASP.NET Core's
    /// HtmlEncoder spells it <c>&amp;#x27;</c>. Same choice, and same reason, as PostLinkCompat.
    /// </summary>
    public static class SelectHelperExtensions
    {
        public static MvcHtmlString Select(this IHtmlHelper helper, string name,
                                           IEnumerable<SelectOption> items, string selected)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<select name='{0}'>", Encode(name));
            foreach (var item in items)
                sb.AppendFormat("<option value='{0}' {2}>{1}</option>",
                                Encode(item.Value),
                                Encode(item.Name),
                                selected == item.Value ? "selected" : "");
            sb.Append("</select>");
            return new MvcHtmlString(sb.ToString());
        }

        /// <summary>Verbatim from HtmlHelperExtensions.cs, like Select above.</summary>
        public static MvcHtmlString BoolSelect(this IHtmlHelper helper, string name, bool? selected, string anyItem)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<select name='{0}'>", Encode(name));
            if (anyItem != null) sb.AppendFormat("<option {1}>{0}</option>", Encode(anyItem), selected == null ? "selected" : "");
            sb.AppendFormat("<option value='True' {0}>Yes</option>", selected == true ? "selected" : "");
            sb.AppendFormat("<option value='False' {0}>No</option>", selected == false ? "selected" : "");

            sb.Append("</select>");
            return new MvcHtmlString(sb.ToString());
        }

        /// <summary>
        /// Verbatim from HtmlHelperExtensions.cs. Note that THIS one walks Enum.GetNames, so its
        /// options come out in numeric order - unlike EnumDropDownListFor, which MVC 5 built from
        /// GetFields and which therefore uses declaration order. Two helpers on the same page
        /// ordering enums differently is the site's behaviour, not a porting choice.
        /// </summary>
        public static MvcHtmlString Select(this IHtmlHelper helper, string name, Type etype, int? selected, string anyItem)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<select name='{0}'>", Encode(name));
            var names = Enum.GetNames(etype);
            var values = (int[])Enum.GetValues(etype);
            if (anyItem != null) sb.AppendFormat("<option {1}>{0}</option>", Encode(anyItem), selected == null ? "selected" : "");
            for (var i = 0; i < names.Length; i++)
                sb.AppendFormat("<option value='{0}' {2}>{1}</option>",
                                Encode(values[i]),
                                Encode(names[i]),
                                selected == values[i] ? "selected" : "");
            sb.Append("</select>");
            return new MvcHtmlString(sb.ToString());
        }

        public static MvcHtmlString EnumCheckboxesFor<TModel, TEnum>(this IHtmlHelper<TModel> htmlHelper,
                                                                     Expression<Func<TModel, IList<TEnum>>> expression,
                                                                     IList<TEnum> hideList = null)
        {
            var listing = ModelValue(htmlHelper, expression);
            var name = htmlHelper.NameFor(expression);
            var sb = new StringBuilder();
            foreach (var val in Enum.GetValues(typeof(TEnum)))
            {
                if (hideList != null && hideList.Contains((TEnum)val)) continue;
                var isSelected = listing == null;
                if (listing != null) isSelected = listing.Contains((TEnum)val);
                sb.AppendFormat("<label><input type='checkbox' name='{0}' value='{1}' {2}/>{3}</label>",
                                name,
                                (int)val,
                                isSelected ? "checked='checked'" : "",
                                Utils.Description((Enum)val));

            }

            return new MvcHtmlString(sb.ToString());
        }

        public static MvcHtmlString MultiSelectFor<TModel, TEnum>(this IHtmlHelper<TModel> htmlHelper,
                                                                  Expression<Func<TModel, IList<TEnum>>> expression,
                                                                  string autocompleteAction,
                                                                  Func<TEnum, MvcHtmlString> objectRenderer)
        {
            var listing = ModelValue(htmlHelper, expression) ?? new List<TEnum>();
            var name = htmlHelper.NameFor(expression);
            var sb = new StringBuilder();
            sb.AppendFormat("<input data-autocomplete='{0}' data-autocomplete-action='add' id='{1}' name='' type='text' value='' class='ui-autocomplete-input' autocomplete='off'><br /><div id='{2}players'></div>", autocompleteAction, name, name);
            foreach (var val in listing)
            {
                var visName = "multivis" + name + val.ToString();
                var hidName = "multihid" + name + val.ToString();
                sb.AppendFormat("<span id='{0}'>{1} <a onclick='$(\"#{2}\").remove();$(\"#{3}\").remove();'><img src='/img/delete_trashcan.png' class='icon16' /></a><br /></span><input type='hidden' name='{4}' id='{5}' value='{6}'>",
                                visName,
                                objectRenderer.Invoke(val),
                                visName,
                                hidName,
                                name,
                                hidName,
                                val);
            }

            return new MvcHtmlString(sb.ToString());
        }

        /// <summary>
        /// What ModelMetadata.FromLambdaExpression(...).Model gave MVC 5 - the live property
        /// value. Null when there is no model at all, which is also what MVC 5 answered.
        /// </summary>
        private static IList<TEnum> ModelValue<TModel, TEnum>(IHtmlHelper<TModel> htmlHelper,
                                                              Expression<Func<TModel, IList<TEnum>>> expression)
        {
            if (htmlHelper?.ViewData == null || htmlHelper.ViewData.Model == null) return null;
            return expression.Compile()(htmlHelper.ViewData.Model);
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value ?? "");

        /// <summary>MVC 5's HtmlHelper.Encode(object), which the 4-argument Select uses on an int.</summary>
        private static string Encode(object value) => WebUtility.HtmlEncode(Convert.ToString(value) ?? "");
    }
}
