using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
  public class HomeController : Controller
  {
    public ActionResult Sitemap()
    {
      var sb = new StringBuilder();
      var db = new ZkDataContext();

      foreach (var x in db.Missions) {
        sb.AppendLine(Url.Action("Detail", "Missions", new { id = x.MissionID }, "http"));
      }

      foreach (var x in db.Resources) {
        sb.AppendLine(Url.Action("Detail", "Maps", new { id = x.ResourceID }, "http"));
      }

      var wikiIndex = new WebClient().DownloadString("http://zero-k.googlecode.com/svn/wiki/");
      var matches = Regex.Matches(wikiIndex, "\"([^\"]+)\"");
      foreach (Match m in matches) {
        if (m.Groups[1].Value.EndsWith(".wiki")) {
          var name = m.Groups[1].Value;
          name = name.Substring(0, name.Length - 5);

          sb.AppendLine(Url.Action("Index", "Wiki", new { node = name }, "http"));
        }
      }

      return Content(sb.ToString());
    }

    //
    // GET: /Home/
    public static string GetMapTooltip(int id)
    {
      var db = new ZkDataContext();
      var sb = new StringBuilder();
      var r = db.Resources.Single(x => x.ResourceID == id);
      sb.Append("<span style='color:black;'>");
      sb.AppendFormat("{0}<br/>", r.InternalName);
      sb.AppendFormat("by {0}<br/>", r.AuthorName);
      if (r.MapIsFfa == true) sb.AppendFormat("<img src='/img/map_tags/ffa.png' class='icon32'  />");
      if (r.MapWaterLevel > 0) sb.AppendFormat("<img src='/img/map_tags/sea{0}.png' class='icon32'  />", r.MapWaterLevel);
      if (r.MapHills > 0) sb.AppendFormat("<img src='/img/map_tags/hill{0}.png' class='icon32' />", r.MapHills);
      if (r.MapIsSpecial == true) sb.AppendFormat("<img src='/img/map_tags/special.png' class='icon32' />");
      if (r.MapIsAssymetrical == true) sb.AppendFormat("<img src='/img/map_tags/assymetrical.png' class='icon32' />");
      sb.Append("<br/>");
      sb.AppendFormat("<img src='http://zero-k.info/Resources/{0}' /><br/>", r.ThumbnailName);
      sb.AppendFormat("Rating: {0}", HtmlHelperExtensions.Stars(null, StarType.GreenStarSmall, r.MapRating).ToHtmlString());

      sb.Append("</span>");
      return sb.ToString();
    }

    public ActionResult Index()
    {
      var spotlight = new UnitSpotlight { };

      try
      {
        var unitData = new WebClient().DownloadString("http://packages.springrts.com/zkmanual/featured.txt");
        var lines = unitData.Lines();
        var r = new Random();
        var line = lines[r.Next(lines.Length)];
        var parts = line.Split('\t');
        spotlight.Unitname = parts[0];
        spotlight.Name = parts[1];
        spotlight.Description = parts[2];
      } catch (Exception ex)
      {
        Trace.TraceError("Error generating unit spotlight: {0}", ex);
      }

      return View(new IndexResult() { Spotlight = spotlight  });
    }

    public class IndexResult
    {
      public UnitSpotlight Spotlight;

    }

    public class UnitSpotlight
    {
      public string Unitname;
      public string Name;
      public string Description;
    }

    public static string GetMissionTooltip(int id)
    {
      var db = new ZkDataContext();
      var sb = new StringBuilder();
      var mis = db.Missions.Single(x => x.MissionID == id);

      sb.Append("<span>");
      sb.AppendFormat("{0}<br/>---<br/>", HttpUtility.HtmlEncode(mis.Description ?? "").Replace("\n", "<br/>"));
      sb.AppendFormat("Players: {0}<br/>", mis.MinToMaxHumansString);
      sb.AppendFormat("<small>{0}</small><br/>", string.Join(",", mis.GetPseudoTags()));
      sb.AppendFormat("Map: {0}<br/>", mis.Map);
      sb.AppendFormat("Game: {0}<br/>", mis.Mod ?? mis.ModRapidTag);
      sb.AppendFormat("Played: {0} times<br/>", mis.MissionRunCount);
      sb.AppendFormat("Rated: {0} times<br/>", mis.Ratings.Count);
      sb.AppendFormat("Comments: {0}<br/>", mis.ForumThread != null ? mis.ForumThread.ForumPosts.Count : 0);
      sb.Append("</span>");

      return sb.ToString();
    }

    [NoCache]
    public ActionResult GetTooltip(string key)
    {
      var args = key.Split(new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
      var ret = "";
      switch (args[0]) {
        case "mission":

          ret = GetMissionTooltip(int.Parse(args[1]));
          break;
        case "map":
          int id;
          if (int.TryParse(args[1], out id)) ret = GetMapTooltip(id);
          else {
            var db = new ZkDataContext();
            ret = GetMapTooltip(db.Resources.Single(x => x.InternalName == args[1]).ResourceID);
          }
          break;
      }
      return Content(ret);
    }


    public ActionResult Logon(string login, string password, string referer)
    {
      var db = new ZkDataContext();

      var acc = db.Accounts.SingleOrDefault(x => x.Name == login);
      if (acc == null) return Content("Invalid login name");
      var hashed = Utils.HashLobbyPassword(password);
      acc = AuthServiceClient.VerifyAccountHashed(login, hashed);
      if (acc == null) return Content("Invalid password");
      else {
        Response.SetCookie(new HttpCookie(GlobalConst.LoginCookieName, login));
        Response.SetCookie(new HttpCookie(GlobalConst.PasswordHashCookieName, hashed));

        return Redirect(referer);
      }
    }
  }
}