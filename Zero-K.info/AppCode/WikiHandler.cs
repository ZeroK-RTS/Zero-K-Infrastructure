using System;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb
{
    public class WikiHandler
    {
        private static Regex reLinks = new Regex(@"(<a.*?>.*?</a>)", RegexOptions.Singleline);

        // ZOMG! this is crazy mix of shit-code awesomeness! Dont look too much here, u can break u eyes!
        private static void StripPieces(
            string fromHeader, string fromAuthor, string fromContent, string to,
            string source, out string header, out string content, out string author)
        {
            header = "";
            content = "";
            author = "";
            var iHeader = source.IndexOf(fromHeader);
            var iAuthor = source.IndexOf(fromAuthor);
            var iAuthorEnd = source.IndexOf("</div>", iAuthor) + 6;
            var iContent = source.IndexOf(fromContent);
            var iTo = source.LastIndexOf(to);

            if (iHeader > -1 && iContent > -1)
            {
                header = source.Substring(iHeader, iContent - iHeader);
                author = source.Substring(iAuthor, iAuthorEnd - iAuthor).Replace("/u/", "http://code.google.com/u/");

                List<string> links = new List<string>();
                foreach (Match m in reLinks.Matches(header))
                {
                    string link = m.Groups[1].Value;
                    if (link.Contains("/p/zero-k/wiki/"))
                    {
                        var lang = link
                            .Substring(0, link.Length - 4) // remove trailing </a>
                            .Split('>').Last();  // get langcode
                        link = link
                            .Replace("wl=", "language=")
                            .Replace(">" + lang + "<", "><")   // replace language text with flag image
                            .Replace("</a>", "<img src=\"/img/flags/" + lang + ".png\" alt=\"" + lang + "\"></img></a>");
                        links.Add(link);
                    }
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
            return piece.Trim();
        }

        private static string FormatWiki(string node, string language, string body, bool isOnlyBody = false)
        {
            string availableLanguages;            
            string author;
            string content;
            StripPieces("<div id=\"wikiheader\"", "<div id=\"wikiauthor\"", "<div id=\"wikicontent\"", "</td>", body, out availableLanguages, out content, out author);
            availableLanguages = FixPiece(availableLanguages);
            availableLanguages = String.IsNullOrEmpty(availableLanguages) ? "" : availableLanguages;
            content = FixPiece(content);

            content = HtmlHelperExtensions.ProcessAtSignTags(content);

            string wikiLink = "http://code.google.com/p/zero-k/wiki/" + node + (String.IsNullOrEmpty(language) ? "" : "?wl=" + language);

            if (isOnlyBody)
                return content;

            return 
                "<div>" + 
                "<span style='float: left; width: 32%; text-align: left;'>" + availableLanguages + "</span>" +
                "<span style='float: left; width: 32%; text-align: center;'><a href='" + wikiLink + "'>edit</a></span>" +
                "<span style='float: left; width: 32%; text-align: right;'>" + author + "</span>" + 
                "</div><div style='clear: both;'></div><br />" + 
                content;
        }

        private static string TryLoadWiki(string node, string language = "", bool isOnlyBody = false)
        {
            string key = "wiki_" + node + "_" + (String.IsNullOrEmpty(language) ? "en" : language);
            var entry = HttpContext.Current.Cache.Get(key) as string;
            if (entry != null) return entry;

            var wc = new WebClient();
            wc.Headers[HttpRequestHeader.AcceptLanguage] = language;
            wc.Encoding = Encoding.UTF8;
            if (String.IsNullOrEmpty(node)) node = "Manual";

            var url = "http://code.google.com/p/zero-k/wiki/" + node;
            var ret = FormatWiki(node, language, wc.DownloadString(url), isOnlyBody);

            HttpContext.Current.Cache.Insert(key, ret, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration);
            return ret;
        }

        public static string LoadWiki(string node, string forceLanguage = "", bool isOnlyBody = false)
        {
            try
            {
                if (String.IsNullOrEmpty(forceLanguage))
                    return TryLoadWiki(node, Global.DisplayLanguage, isOnlyBody);
                return TryLoadWiki(node, forceLanguage, isOnlyBody);
            }
            catch (System.Exception ex)
            {
                return string.Format("Error loading {0} : {1}", node, ex.Message);
            }
        }
    }
}