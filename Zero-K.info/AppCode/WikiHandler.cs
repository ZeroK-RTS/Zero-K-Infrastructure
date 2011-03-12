using System;
using System.Net;
using System.Web;
using System.Web.Caching;

namespace ZeroKWeb
{
	public class WikiHandler
	{
		public static string LoadWiki(string node)
		{
			var entry = HttpContext.Current.Cache.Get("wiki_" + node) as string;
			if (entry != null) return entry;

			var wc = new WebClient();
			if (String.IsNullOrEmpty(node)) node = "Manual";

			var ret = wc.DownloadString("http://code.google.com/p/zero-k/wiki/" + node);

			var idx = ret.IndexOf("<div id=\"wikiheader\"");
			if (idx > -1) idx = ret.IndexOf("</span>", idx) + 7;
			var idx2 = ret.LastIndexOf("<div class=\"collapse\">");
			if (idx2 == -1) idx2 = ret.LastIndexOf("<br>");

			var temp = ret.LastIndexOf("</td>", idx2 - 30);
			if (temp != -1) idx2 = temp;

			if (idx > -1 && idx2 > -1) ret = ret.Substring(idx, idx2 - idx);

			ret = ret.Replace("href=\"/p/zero-k/wiki/", "href =\"/Wiki.mvc/");
			ret = ret.Replace("href=\"/", "href=\"http://code.google.com/");

			ret = String.Concat("<div class=''>", ret);

			HttpContext.Current.Cache.Insert("wiki_" + node, ret, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration);
			return ret;
		}
	}
}