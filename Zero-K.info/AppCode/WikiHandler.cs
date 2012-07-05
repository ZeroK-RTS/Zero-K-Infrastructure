using System;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using ZkData;

namespace ZeroKWeb
{
    public class WikiHandler
    {
        public static CultureInfo ResolveCulture()
        {
            string[] languages = HttpContext.Current.Request.UserLanguages;

            if (languages == null || languages.Length == 0)
                return null;

            try
            {
                string language = languages[0].ToLowerInvariant().Trim();
                return CultureInfo.CreateSpecificCulture(language);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

		
        public static RegionInfo ResolveCountry()
        {
            CultureInfo culture = ResolveCulture();
            if (culture != null)
                return new RegionInfo(culture.LCID);

            return null;
        }

        public static string ResolveLanguage()
        {
			if (Global.IsAccountAuthorized)
			{
				var db = new ZkDataContext();
				var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);            
				string manualLanguage = acc == null ? null : acc.Country.ToLower();
				
				if (!String.IsNullOrEmpty(manualLanguage))
					return manualLanguage;
			}

            RegionInfo ri = ResolveCountry();
            if (ri != null && !String.IsNullOrEmpty(ri.TwoLetterISORegionName))
                return ri.TwoLetterISORegionName;

            return "en";
        }

        public static string FormatWiki(string str)
        {
            var idx = str.IndexOf("<div id=\"wikicontent\"");
            var idx2 = str.LastIndexOf("</td>");

            if (idx > -1 && idx2 > -1) str = str.Substring(idx, idx2 - idx);

            str = str.Replace("href=\"/p/zero-k/wiki/", "href =\"/Wiki/");
            str = str.Replace("href=\"/", "href=\"http://code.google.com/");

            return str;
        }

        public static string TryLoadWiki(string node, string language = "")
        {
            string key = "wiki_" + node + "_" + (String.IsNullOrEmpty(language) ? "en" : language);
            var entry = HttpContext.Current.Cache.Get(key) as string;
            if (entry != null) return entry;

            var wc = new WebClient();
            wc.Headers[HttpRequestHeader.AcceptLanguage] = language;
            wc.Encoding = Encoding.UTF8;
            if (String.IsNullOrEmpty(node)) node = "Manual";

            var url = "http://code.google.com/p/zero-k/wiki/" + node;
            var ret = FormatWiki(wc.DownloadString(url));

            HttpContext.Current.Cache.Insert(key, ret, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration);
            return ret;
        }

        public static string LoadWiki(string node)
        {
            try
            {
                return TryLoadWiki(node, ResolveLanguage());
            }
            catch (System.Exception ex)
            {
                return string.Format("Error loading {0} : {1}", node, ex.Message);
            }
        }
    }
}