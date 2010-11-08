using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ZkData;

namespace System.Web.Mvc
{
	public enum StarType
	{
		RedStarSmall,
		GreenStarSmall,
		WhiteStarSmall
	}

	public class SelectOption
	{
		public string Value;
		public string Name;
	}



	public static class HtmlHelperExtensions
	{
		public static string Stars(this HtmlHelper helper, StarType type, double? rating)
		{
			if (rating.HasValue) return string.Format("<span class='{0}' style='width:{1}px'></span>", type,(int)(rating *14.0));
			else return string.Format("<span class='WhiteStarSmall' style='width:70px'></span>");
		}

		public static string Select(this HtmlHelper helper, string name, IEnumerable<SelectOption> items, string selected)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("<select name='{0}'>", helper.Encode(name));
			foreach (var item in items)
			{
				sb.AppendFormat("<option value='{0}' {2}>{1}</option>",
				                helper.Encode(item.Value),
				                helper.Encode(item.Name),
				                selected == item.Value ? "selected" : "");
			}
			sb.Append("</select>");
			return sb.ToString();
		}

	}
}