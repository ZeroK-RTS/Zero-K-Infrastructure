#region using

using System;
using System.Linq;
using System.Text;
using System.Web;
using JetBrains.Annotations;
using PlanetWarsShared;

#endregion

public partial class MainView : CommonMasterPageBase
{
    protected void Page_Init(object sender, EventArgs e)
    {
        Response.Cache.SetCacheability(HttpCacheability.NoCache);

        if (Request[btnLogout.UniqueID] != null)
        {
            Response.SetCookie(new HttpCookie("password", ""));
            Globals.CurrentLogin = null;
        }
        else {

            if (!IsPostBack && Globals.CurrentLogin == null) {
                tbLogin.Text = Request["login"];
                Globals.TryLogin(Request["login"], Request["password"]); // first visit -try login using cookies/params}
            } else if (Globals.CurrentLogin == null) {
                Globals.TryLogin(Request[tbLogin.UniqueID], Request[tbPassword.UniqueID]);
            }
            if (Globals.Player != null) {
                FillUserInfo();
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        SetGeneralGalaxyInfo();
        RenderLeftBar();
    }

    void SetGeneralGalaxyInfo()
    {
        lbTurn.Text = Globals.Galaxy.Turn.ToString();
        lbAttacks.Text = HtmlRenderingExtensions.GetFactionLink(Globals.Galaxy.OffensiveFaction.Name);
    }

    protected void btnLogin_Click(object sender, EventArgs e)
    {
        if (Globals.CurrentLogin == null) {
            MessageBox.Show("Invalid login or password");
        } else {
            Response.Redirect("player.aspx?name=" + Globals.CurrentLogin.Login);
        }
    }

    void FillUserInfo()
    {
        lbPassword.Visible = false;
        tbLogin.Visible = false;
        tbPassword.Visible = false;
        btnLogin.Visible = false;
        lbLogin.Visible = false;
        btnLogout.Visible = true;
        litPlayerInfo.Visible = true;
        Response.SetCookie(new HttpCookie("login", Globals.CurrentLogin.Login) {Expires = DateTime.Now.AddDays(30)});
        Response.SetCookie(
            new HttpCookie("password", Globals.CurrentLogin.Password) {Expires = DateTime.Now.AddDays(30)});
        litPlayerInfo.Text = Globals.Player.ToHtml();
        litPlayerInfo.Text +=
            string.Format(
                "&nbsp;&nbsp;|&nbsp;&nbsp;Credits: <span style='color:aqua'>{0}</span>€&nbsp;&nbsp;|&nbsp;&nbsp;<a href='settings.aspx'>Settings</a>",
                Globals.Player.MetalAvail);
    }

    void RenderLeftBar()
    {
        var sb = new StringBuilder();

        var planetCounts = from p in Globals.Galaxy.Planets
                           where p.FactionName != null
                           group p by p.FactionName
                           into g orderby g.Key select new {Faction = g.Key, Count = g.Count()};

        int totalCount = 0;
        foreach (var ts in planetCounts) {
            totalCount += ts.Count;
        }

        sb.AppendFormat("<table cellpadding='0' cellspacing='0'><tr width='200' style='color:white;'>");
        var colorTable = new[] {"Blue", "Red"};
        int cnt = 0;
        foreach (var pk in planetCounts) {
            sb.AppendFormat(
                "<td style='width:{0}px;height:20px;text-align:center;background-color:{3}'>{2}{1}</td>",
                pk.Count*200/totalCount,
                pk.Count,
                HtmlRenderingExtensions.GetFactionImage(pk.Faction),
                colorTable[cnt++%colorTable.Length]);
        }
        sb.AppendFormat("</tr></table>\n");

        foreach (var faction in Globals.Galaxy.Factions) {
            var plList = Globals.Galaxy.Players.Where(p => p.FactionName == faction.Name);
            var list = plList.ToList();

            sb.AppendFormat("<hr/>{0}<hr/>\n", HtmlRenderingExtensions.GetFactionLink(faction.Name));
            list.Sort(Player.CompareTo);
            for (int i = 1; i <= Math.Min(10, list.Count); i++) {
                var pl = list[list.Count - i];
                sb.AppendFormat("{0}. {1} ({2:f1})<br/>\n", i, pl.ToHtml(), pl.RankPoints);
            }
        }

        sb.AppendFormat("<hr/><a href='awardhall.aspx'>Awards Hall of Fame</a>\n");

        sb.AppendFormat("<hr/><a href='events.aspx'>Last events</a><hr/>\n");

        Globals.Galaxy.Events.Where(x => !x.IsHiddenFrom(Globals.MyFaction)).Reverse().Take(10).ForEach(
            x => sb.AppendFormat("{0}: <span style='color: white;'>{1}</span><br/>\n", x.TimeOffset(Globals.Player, true), x.ToHtml()));

        if (Globals.Player != null) {
            sb.AppendFormat(
                "<hr/><span style='color:white;'><a href='chat.aspx'>#{0} channel log</a></span><hr/>\n<span style='color:white;font-size:xx-small;'>",
                Globals.MyFaction);
            var chatEvents = Globals.Galaxy.GetFaction(Globals.MyFaction).ChatEvents;
            for (int i = Math.Max(chatEvents.Count - 100, 0); i < chatEvents.Count; i++) {
                var ce = chatEvents[i];
                sb.AppendFormat(
                    "<span style='color:aqua'>{0}</span>&nbsp;{1}<br/>\n",
                    Server.HtmlEncode(ce.Name),
                    Server.HtmlEncode(ce.Text));
            }
            sb.Append("</span>");
        }

        litPlayers.Text = sb.ToString();
    }

    [StringFormatMethod("format")]
    public override void AppendJavascript(string format, params object[] parameters)
    {
        litJavascript.Text += string.Format(format, parameters);
    }

}