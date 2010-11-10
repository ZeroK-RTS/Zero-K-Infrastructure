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
		public static MvcHtmlString Stars(this HtmlHelper helper, StarType type, double? rating)
		{
			if (rating.HasValue) return new MvcHtmlString(string.Format("<span class='{0}' style='width:{1}px'></span>", type,(int)(rating *14.0)));
			else return new MvcHtmlString(string.Format("<span class='WhiteStarSmall' style='width:70px'></span>"));
		}

		public static MvcHtmlString Select(this HtmlHelper helper, string name, IEnumerable<SelectOption> items, string selected)
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
		  return new MvcHtmlString(sb.ToString());
		}

		public static MvcHtmlString PrintAccount(this HtmlHelper helper, Account account)
		{
			return new MvcHtmlString(string.Format("<img src='/img/flags/{0}.png' class='flag'><img src='/img/ranks/{1}.png'  class='icon16'>{2}", account.Country, account.LobbyTimeRank+1, account.Name));
		}

    public static MvcHtmlString PrintLines(this HtmlHelper helper, string text)
    {
      return new MvcHtmlString(helper.Encode(text).Replace("\n", "<br/>"));
    }

    public static MvcHtmlString PrintLines(this HtmlHelper helper, IEnumerable<object> lines)
    {
      var sb = new StringBuilder();
      foreach (var line in lines)
      {
        sb.AppendFormat("{0}<br/>", line);
      }
      return new MvcHtmlString(sb.ToString());
    }


		public static string ToAgoString(this DateTime utcDate)
		{
			return ToAgoString(DateTime.UtcNow.Subtract(utcDate));
		}

		public static string ToAgoString(this TimeSpan timeSpan)
		{
			if (timeSpan.TotalMinutes < 2) return string.Format("{0} seconds ago", (int)timeSpan.TotalSeconds);
			if (timeSpan.TotalHours < 2) return string.Format("{0} minutes ago", (int)timeSpan.TotalMinutes);
			if (timeSpan.TotalDays < 2) return string.Format("{0} hours ago", (int)timeSpan.TotalHours);
			if (timeSpan.TotalDays < 60) return string.Format("{0} days ago", (int)timeSpan.TotalDays);
			return string.Format("{0} months ago", (int)(timeSpan.TotalDays / 30));
		}


	}
}