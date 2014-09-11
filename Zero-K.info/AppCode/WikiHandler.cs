using System;
using System.Diagnostics;
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
                    else if (link.Contains("/ZeroK-RTS/Zero-K/wiki/"))
                    {
                        // TODO
                    }
                }
                header = String.Join(", ", links);
            }

            if (iContent > -1 && iTo > -1)
                content = source.Substring(iContent, iTo - iContent);
        }

        private static string GetGitHubWikiAuthor(string html)
        {
            Regex rex = new Regex("class=\"author\">(.*?)</a> edited this page <time .*>(.*?)</time>");
            //var match = rex.Match(html);
            //return "Last edited by " + match.Groups["name"].Value + " on " + match.Groups["timeStamp"].Value;
            var match = rex.Match(html);
            string[] names = rex.GetGroupNames();
            string name = names.Length > 0 ? match.Groups[names[1]].Value : "unknown author";
            string time = names.Length > 1 ? match.Groups[names[2]].Value : "at unknown time";
            name = String.Format("<a href=\"http://github.com/{0}\">{0}</a>", name);
            return String.Format("Updated {1} by {0}", name, time);
        }
        
        private static string FixPiece(string piece)
        {
            piece = piece.Replace("href=\"/p/zero-k/wiki/", "href =\"/Wiki/");
            piece = piece.Replace("href=\"/", "href=\"http://code.google.com/");
            return piece.Trim();
        }

        private static string FormatGoogleCodeWiki(string node, string language, string body, bool isOnlyBody = false)
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
                "<span style='float: left; width: 32%; text-align: center;'>" + author + "</span>" + 
                "<span style='float: left; width: 32%; text-align: right;'><a href='" + wikiLink + "'>edit</a></span>" +
                "</div><div style='clear: both;'></div><br />" + 
                content;
        }

        private static string FormatGitHubWiki(string node, string html, string raw, bool isOnlyBody = false)
        {
            string availableLanguages;
            string author = GetGitHubWikiAuthor(html);
            string content;
            string wikiLink = "https://github.com/ZeroK-RTS/Zero-K/wiki/" + node;

            // handle Github flavouring
            Regex rex = new Regex(@"\[\[(.*)?\|(.*)?\]\]", RegexOptions.IgnoreCase);
            content = rex.Replace(raw, "[$1]($2)");

            content = MarkdownHelper.MarkdownRaw(content);
            content = HtmlHelperExtensions.ProcessAtSignTags(content);

            if (isOnlyBody)
                return content;

            return
                "<div>" +
                "<span style='float: left; width: 50%; text-align: center;'>" + author + "</span>" + 
                "<span style='float: left; width: 50%; text-align: center;'><a href='" + wikiLink + "'>edit</a></span>" +
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
            HttpWebResponse response;
            string ret = string.Empty;
            bool success = false;

            if (String.IsNullOrEmpty(node)) node = "Manual";
            /*
            if (!success)
            {
                string gitHubURLraw = "https://raw.githubusercontent.com/wiki/ZeroK-RTS/Zero-K/" + node + ".md";
                string gitHubURL = "http://github.com/ZeroK-RTS/Zero-K/wiki/" + node;
                HttpWebRequest checkGithub = (HttpWebRequest)HttpWebRequest.Create(gitHubURLraw);
                checkGithub.Method = "HEAD";
                try
                {
                    response = (HttpWebResponse)checkGithub.GetResponse();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        ret = FormatGitHubWiki(node, wc.DownloadString(gitHubURL), wc.DownloadString(gitHubURLraw), isOnlyBody);
                        success = true;
                    }
                }
                catch (Exception ex) { }  // next!
            }
             */

            if (!success)
            {
                string googleCodeURL = "http://code.google.com/p/zero-k/wiki/" + node;
                HttpWebRequest checkGoogleCode = (HttpWebRequest)WebRequest.Create(googleCodeURL);
                checkGoogleCode.Method = "HEAD";
                response = (HttpWebResponse)checkGoogleCode.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    ret = FormatGoogleCodeWiki(node, language, wc.DownloadString(googleCodeURL), isOnlyBody);
                    success = true;
                }
            }

            if (success) HttpContext.Current.Cache.Insert(key, ret, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration);
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
                Trace.TraceWarning("Error loading {0} : {1}", node, ex.Message);
                return string.Format("Error loading {0} : {1}", node, ex.Message);
            }
        }
    }
}