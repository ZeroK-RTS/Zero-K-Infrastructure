using System;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZkData;

namespace ZeroKWeb
{
    public class WikiHandler
    {
        private static Regex reLinks = new Regex(@"(<a.*?>.*?</a>)", RegexOptions.Singleline);

        // ZOMG! this is crazy mix of shit-code awesomeness! Dont look too much here, u can break u eyes!
        private static void StripPieces(string fromHeader, string fromContent, string to, string source, out string header, out string content)
        {
            header = "";
            content = "";
            var iHeader = source.IndexOf(fromHeader);
            var iContent = source.IndexOf(fromContent);
            var iTo = source.LastIndexOf(to);

            if (iHeader > -1 && iContent > -1)
            {
                header = source.Substring(iHeader, iContent - iHeader);

                List<string> links = new List<string>();
                foreach (Match m in reLinks.Matches(header))
                {
                    string link = m.Groups[1].Value;
                    if (link.Contains("/p/zero-k/wiki/"))
                        links.Add(link.Replace("wl=", "language="));
                }
                header = String.Join(", ", links);
            }

            if (iContent > -1 && iTo > -1)
                content = source.Substring(iContent, iTo - iContent);
        }
        
        private static string FixPiece(string piece)
        {
            piece = piece.Replace("href=\"/p/zero-k/wiki/", "href =\"/Wiki/");
            piece = piece.Replace("href=\"/", "href=\"http://code.google.com/");
            return piece;
        }

        private static string FormatWiki(string str)
        {
            string header;
            string content;
            StripPieces("<div id=\"wikiheader\"", "<div id=\"wikicontent\"", "</td>", str, out header, out content);

            return FixPiece(header) + "<br />" + FixPiece(content);
        }

        private static string TryLoadWiki(string node, string language = "")
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

        public static string LoadWiki(string node, string forceLanguage = "")
        {
            try
            {
                if (String.IsNullOrEmpty(forceLanguage))
                    return TryLoadWiki(node, Global.DisplayLanguage);
                return TryLoadWiki(node, forceLanguage);
            }
            catch (System.Exception ex)
            {
                return string.Format("Error loading {0} : {1}", node, ex.Message);
            }
        }
    }
}