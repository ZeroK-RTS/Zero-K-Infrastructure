using System;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.Text;

namespace ZeroKWeb
{
	public class WikiHandler
	{
		public static string LoadWiki(string node)
		{
			try {
				var entry = HttpContext.Current.Cache.Get("wiki_" + node) as string;
				if (entry != null) return entry;

				var wc = new WebClient();
        wc.Encoding = Encoding.UTF8;
				if (String.IsNullOrEmpty(node)) node = "Manual";

				var ret = wc.DownloadString("http://code.google.com/p/zero-k/wiki/" + node);

				var idx = ret.IndexOf("<div id=\"wikicontent\"");
				var idx2 = ret.LastIndexOf("</td>");

				if (idx > -1 && idx2 > -1) ret = ret.Substring(idx, idx2 - idx);

				ret = ret.Replace("href=\"/p/zero-k/wiki/", "href =\"/Wiki/");
				ret = ret.Replace("href=\"/", "href=\"http://code.google.com/");

				HttpContext.Current.Cache.Insert("wiki_" + node, ret, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration);
				return ret;
			} catch (Exception ex) {
				return string.Format("Error loading {0} : {1}", node, ex.Message);
			}
		}
	}
}