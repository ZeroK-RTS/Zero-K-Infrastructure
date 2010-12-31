using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace ZeroKWeb
{
  public class WikiHandler
  {
    public static string LoadWiki(string node) {
      var wc = new WebClient();
      if (String.IsNullOrEmpty(node)) node = "Intro";

      var ret = wc.DownloadString("http://code.google.com/p/zero-k/wiki/" + node);

      var idx = ret.IndexOf("<div id=\"wikiheader\"");
      if (idx > -1) idx = ret.IndexOf("</span>", idx) + 7;
      var idx2 = ret.LastIndexOf("<div class=\"collapse\">");
      if (idx2 == -1) idx2 = ret.LastIndexOf("<br>");

      if (idx > -1 && idx2 > -1) ret = ret.Substring(idx, idx2-idx);

      ret = ret.Replace("href=\"/p/zero-k/wiki/", "href=\"");
      ret = ret.Replace("href=\"/", "href=\"http://code.google.com/");


      ret = String.Concat("<div class=''><div>", ret);
      return ret;
    }
  }
}