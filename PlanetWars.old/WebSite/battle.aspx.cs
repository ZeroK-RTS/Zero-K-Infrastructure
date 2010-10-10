using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PlanetWarsShared;
using PlanetWarsShared.Events;

public partial class battle : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        int turn = int.Parse(Request["turn"]);
        BattleEvent b = null;
        foreach (var ev in Globals.Galaxy.Events) {
            if (ev is BattleEvent && ev.Turn == turn) {
                b = ev as BattleEvent;
                break;
            }
        }
        if (b == null) return;

        var planet = Globals.Galaxy.GetPlanet(b.PlanetID);
        var loggedInPlayer = Globals.Player;
        lbTitle.Text = "Battle on " + planet.Name + " turn " + b.Turn;
        //lbTime.Text = b.Time.ToString();
        lbTime.Text = b.TimeOffset(loggedInPlayer);
        lbAttacker.Text = HtmlRenderingExtensions.GetFactionLink(b.Attacker);
        lbWinner.Text = HtmlRenderingExtensions.GetFactionLink(b.Victor);
        lbPlanet.Text = planet.ToHtml();

        var sb = new StringBuilder();
        sb.Append("<table style='border:0'>");

        int totalTime = 0;
        foreach (var p in b.EndGameInfos.Where(x => x.Spectator == false).OrderBy(x=>x.Side)) {
            int pTime = 0;
            if (!p.AliveTillEnd) {
                if (p.LoseTime > 0) pTime = p.LoseTime; else 
                if (p.LeaveTime > 0) pTime = p.LeaveTime;
            }
            if (pTime > totalTime) totalTime = pTime;

            string awards = "";
            var player = Globals.Galaxy.GetPlayer(p.Name);
            foreach (var aw in player.Awards.Where(
                x => x.Turn == b.Turn && x.Round == b.Round)) {
                awards += aw.Text + "<br/>";
            }
            string fixedFactionName =
                Globals.Galaxy.Factions.Single(x => x.Name.ToLower() == p.Side.ToLower()).Name;

            sb.AppendFormat(
                "<tr><td>{0}</td><td>{1}</td><td></td><td>{2}</td><td>{3}</td></tr>",
                player.ToHtml(),
                HtmlRenderingExtensions.GetFactionLink(fixedFactionName),
                pTime.ToTime(),
                awards);}

        lbLength.Text = totalTime.ToTime();
        sb.Append("</table>");
        sb.AppendFormat("<img src='{0}' style='width:200px; margin:4px; border: 1px aqua solid;'><br/>",HtmlRenderingExtensions.GetMinimapUrl(planet.MapName));
        litDetails.Text = sb.ToString();


    }
}
