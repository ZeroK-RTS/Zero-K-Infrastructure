using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace System.Web.Mvc
{
    /// <summary>
    /// MVC 5's <c>EnumHelper.GetSelectList</c>, which Maps/Detail passes to
    /// <c>Html.DropDownList</c>. ASP.NET Core has <c>IHtmlHelper.GetEnumSelectList&lt;T&gt;()</c>,
    /// which is a different function: it takes a generic parameter rather than a Type, and it
    /// does not do the thing below with null.
    ///
    /// Options come out the same way <c>EnumDropDownListFor</c> builds them - declaration order,
    /// <c>[Display]</c> over the field name, the NUMBER as the value - because MVC 5 built both
    /// from the same helper. See EnumDropDownListCompat.cs for what settled each of those.
    ///
    /// **What only a capture could have given: what a null value does.** Three shapes were
    /// recorded, and the third is the one that matters:
    ///
    ///     GetSelectList(Support, Featured)  -> Featured selected, four options
    ///     GetSelectList(Support, null)      -> None selected, four options    (Support has a 0)
    ///     GetSelectList(Plain, null)        -> &lt;option selected value="0"&gt;&lt;/option&gt; PREPENDED
    ///     GetSelectList(Plain, Planetwars)  -> Planetwars selected, three options, NO extra
    ///
    /// So a null value is rendered as the enum's zero, and when no member IS zero, MVC 5
    /// synthesises an empty entry to hold the selection rather than leaving nothing selected.
    /// The fourth shape is what shows that entry belongs to the null and not to the enum -
    /// without it, "this enum has no zero member" would have been an equally good reading and
    /// would have put a stray blank option on every map's support-level dropdown.
    /// </summary>
    public static class EnumHelper
    {
        public static IList<SelectListItem> GetSelectList(Type enumType, Enum value)
        {
            var items = new List<SelectListItem>();
            var selected = value == null ? "0" : value.ToString("d");
            var hasZero = false;

            // GetFields, not Enum.GetNames: declaration order. See EnumDropDownListCompat.
            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var fieldValue = ((Enum)field.GetValue(null)).ToString("d");
                if (fieldValue == "0") hasZero = true;
                items.Add(new SelectListItem
                {
                    Text = field.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? field.Name,
                    Value = fieldValue,
                    Selected = fieldValue == selected,
                });
            }

            // Only when the value is null AND nothing is zero - see the class note.
            if (value == null && !hasZero)
                items.Insert(0, new SelectListItem { Text = "", Value = "0", Selected = true });

            return items;
        }
    }
}
