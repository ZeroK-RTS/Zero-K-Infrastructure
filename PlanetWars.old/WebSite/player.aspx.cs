#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PlanetWarsShared;

#endregion

public partial class PlayerPage : CommonPageBase
{
    #region Fields

    private Player displayedPlayer;
    private bool isAllied;
    private bool isOwner;

    #endregion

    #region Other methods

    protected void btnSendBp_Click(object sender, EventArgs e)
    {
        if (!isOwner && isAllied) {
            double ammount;
            double.TryParse(tbBpAmmount.Text, out ammount);
            string message;
            if (!Globals.Server.SendAid(displayedPlayer.Name, ammount, Globals.CurrentLogin, out message)) MessageBox.Show(message);
            else {
                Globals.ResetGalaxy();
                displayedPlayer = Globals.Galaxy.GetPlayer(displayedPlayer.Name);
                DisplayEvents();
                DisplayBuildPowerTable(displayedPlayer);
                tbBpAmmount.Text = "";
            }
        }
    }

    private string ColorNumber(double number)
    {
        string color = "yellow";
        if (number > 0) color = "green";
        else if (number < 0) color = "red";
        return string.Format("<font color='{1}'>{0}</font>", number, color);
    }

    private void DisplayBuildPowerTable(Player p)
    {
        StringBuilder sb;
        sb = new StringBuilder();
        sb.AppendFormat("<table>\n");
        sb.AppendFormat("<tr style='Font-size:medium;'><th>Stuff</th><th>Amount</th></tr>\n");
        sb.AppendFormat("<tr><td>Rank points</td><td style='Color:aqua;'>{0}</td></tr>", p.RankPoints);
        sb.AppendFormat("<tr><td>Victories</td><td style='Color:aqua;'>{0}</td></tr>", p.Victories);
        sb.AppendFormat("<tr><td>Defeats</td><td style='Color:aqua;'>{0}</td></tr>", p.Defeats);
        sb.AppendFormat("<tr><td>Streak victories</td><td style='Color:aqua;'>{0}</td></tr>", p.MeasuredVictories);
        sb.AppendFormat("<tr><td>Streak defeats</td><td style='Color:aqua;'>{0}</td></tr>", p.MeasuredDefeats);
        sb.AppendFormat("<tr><td>Skill</td><td style='Color:aqua;'>{0:f0}</td></tr>", p.Elo);
        sb.AppendFormat("<tr><td>Skill 1v1</td><td style='Color:aqua;'>{0:f0}</td></tr>", p.Elo1v1);
        sb.AppendFormat("<tr><td>Clout (wars led)</td><td style='Color:aqua;'>{0}</td></tr>", p.Clout);
        sb.AppendFormat("<tr><td>Aid recieved</td><td style='Color:aqua;'>{0}</td></tr>", ColorNumber(p.RecievedMetal));
        sb.AppendFormat("<tr><td>Aid sent</td><td style='Color:aqua;'>{0}</td></tr>", ColorNumber(-p.SentMetal));
        sb.AppendFormat("<tr><td align='right'>Credits earned</td><td align='right'><font size='+1'><b>{0}€</b></font></td></tr>", ColorNumber(p.MetalEarned));
        sb.AppendFormat("<tr><td align='right'>Credits spent</td><td align='right'><font size='+1'><b>{0}€</b></font></td></tr>", ColorNumber(-p.MetalSpent));
        sb.AppendFormat("<tr><td align='right'><font size='+1'>CREDITS</font></td><td align='right'><font size='+1'><b>{0}€</b></font></td></tr>", ColorNumber(p.MetalAvail));
        sb.AppendFormat("</table>\n");
        litBuildpower.Text = sb.ToString();
    }

    private void DisplayEvents()
    {
        var sb = new StringBuilder();
        
        var p = Globals.Player;
        Globals.Galaxy.Events.Where(x => x.IsPlayerRelated(displayedPlayer.Name) && !x.IsHiddenFrom(Globals.MyFaction)).Reverse().ForEach(x => sb.AppendFormat("{0}: {1}<br/>\n", x.TimeOffset(p), x.ToHtml()));

        litEvents.Text = sb.ToString();
    }

    private void DisplayUpgrades(Player p, bool isOwner)
    {
        var sb = new StringBuilder();
        var allUpgrades = new List<UpgradeDef>(new Upgrades().UpgradeDefs.Where(u => u.FactionName == p.FactionName));
        allUpgrades.Sort((a, b) =>
                          {
                              int comp = a.Division.CompareTo(b.Division);
                              if (comp == 0) comp = a.Branch.CompareTo(b.Branch);
                              if (comp == 0) comp = a.Level.CompareTo(b.Level);
                              return comp;
                          });

        List<UpgradeDef> allOwned;
        if (!Globals.Server.UpgradeData.TryGetValue(p.Name, out allOwned)) allOwned = new List<UpgradeDef>();

        var avail = Globals.Server.GetAvailableUpgrades(Globals.Galaxy, p.Name).Select(ud => ud.ID);

        string od = "";
        string ob = "";

        foreach (var def in allUpgrades) {
            if (od != def.Division) {
                od = def.Division;
                ob = "";
                sb.AppendFormat("<h2><span style='color:aqua'>{0}</span> division</h2>\n", def.Division);
            }
            if (ob != def.Branch) {
                if (ob != "") sb.Append("</div>\n");
                ob = def.Branch;
                sb.AppendFormat("<div><h3><span style='color:aqua'>{0}</span> branch</h3>", def.Branch);
            }

            bool isAvail = avail.Contains(def.ID);
            var purchased = allOwned.Where(pu => pu.ID == def.ID).SingleOrDefault();

            string style = "margin: 5px; padding: 2px; width: 600px;";
            if (isAvail || purchased != null) style += "border: 2px green";
            else style += "border: 2px red";
            if (purchased != null) style += " solid";
            else style += " dashed";

            // title of the upgrade
            sb.AppendFormat("<div style='{0}'><span style='color: aqua;'>{1}</span> level {2} (cost <span style='color:aqua'>{4}</span>) - {3}", style, def.Branch, def.Level, def.Description, def.Cost);


            // already purchased items
            if (purchased != null) sb.AppendFormat("<br/>Owns: {0}  ({1} purchased, {2} died)\n", purchased.UnitChoiceHumanReadable, purchased.Purchased, purchased.Died);

            // buy new upgade options
            if (isOwner && isAvail && purchased == null) {
                sb.AppendFormat("<br/>\n");
                if (def.UnitDefs.Count > 0) {
                    sb.AppendFormat("<select name='ud{0}'>\n", def.ID);

                    foreach (var unitDef in def.UnitDefs) {
                        sb.AppendFormat("<option value='{1}' {2}>{0}</option>", unitDef.FullName, unitDef.Name, (purchased!=null && purchased.UnitChoice == unitDef.Name) ? "selected":"");
                    }
                    sb.AppendFormat("</select>\n");
                }
                sb.AppendFormat("<input type='submit' name='buy{0}' value='Unlock'> (Cost <font color='aqua'>{1}</font>)\n", def.ID, def.Cost);
            }

            sb.AppendFormat("</div>\n");
        }
        sb.Append("</div>\n");
        litUpgrades.Text = sb.ToString();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        displayedPlayer = Globals.Galaxy.GetPlayer(Request["name"]);
        if (displayedPlayer == null) return;

        isOwner = false;
        isAllied = false;
        if (Globals.Player != null) {
            if (Globals.Player.Name == displayedPlayer.Name) isOwner = true;
            if (Globals.Player.FactionName == displayedPlayer.FactionName) isAllied = true;
        }

        if (IsPostBack) {
            if (isOwner) {
                PurchaseUpgradeCheck();
                Globals.ResetGalaxy();
                displayedPlayer = Globals.Galaxy.GetPlayer(Request["name"]);
            }
        } else {
            if (!isOwner && isAllied) {
                lbSendBp.Visible = true;
                tbBpAmmount.Visible = true;
                btnSendBp.Visible = true;
            }
            var planet = Globals.Galaxy.GetPlanet(displayedPlayer.Name);
            lbPlanet.Text = planet.ToHtml();
            lbFaction.Text = HtmlRenderingExtensions.GetFactionLink(displayedPlayer.FactionName);
        }

        imgRank.ImageUrl = displayedPlayer.Rank.GetImageUrl();
        lbName.Text = displayedPlayer.Name + HtmlRenderingExtensions.GetFactionImage(displayedPlayer.FactionName);
        lbRankName.Text = displayedPlayer.RankText;

        DisplayAwards(displayedPlayer);


        DisplayEvents();
        DisplayBuildPowerTable(displayedPlayer);
        DisplayUpgrades(displayedPlayer, isOwner);
    }

    void DisplayAwards(Player p)
    {
        var sb = new StringBuilder();
        var js = new StringBuilder();
        int cnt = 0;

        js.Append("<script type='text/javascript'>\nfunction AddHovers() {\n");

        foreach(var a in p.Awards.OrderBy(x=>x.IssuedOn)) {
            sb.AppendFormat(
                "<img src='awards/trophy_{0}.png' id='award{1}' style='border: 2px {2} solid; margin: 1px;'>", a.Type, cnt, a.Type != "friend" ? "green" : "red");

            var loggedInPlayer = Globals.Player;
            string issuedOn = "";
            if (loggedInPlayer != null)
            {
                DateTime newTime = DateTime.SpecifyKind(a.IssuedOn, DateTimeKind.Utc);
                issuedOn += TimeZoneInfo.ConvertTimeFromUtc(newTime, p.LocalTimeZone) + " (" + p.LocalTimeZone.BaseUtcOffset.ToString().TrimEnd('0').TrimEnd(':') + ")";
            }
            else
            {
                issuedOn += a.IssuedOn;
            }
            
            string htmltext =
                String.Format(
                    "<div class=\"popup\"><img src=\"awards/trophy_{0}.png\">{1}<br/>Gained {2} on {3}",
                    a.Type,
                    a.Text.Replace("'","`"),
                    issuedOn, Globals.Galaxy.GetPlanet(a.PlanetID).Name);

            js.AppendFormat("PW.AddTooltip('award{0}', '{1}', null);\n", cnt, htmltext);

            cnt++;
        }

        js.Append("\n}\naddLoadEvent(AddHovers);\n</script>\n");
        litAwards.Text = sb.ToString();
        litJs.Text = js.ToString();
    }

    private void PurchaseUpgradeCheck()
    {
        foreach (var rf in Request.Form.Keys) {
            if (rf.ToString().StartsWith("buy")) {
                int ud = int.Parse(rf.ToString().Substring(3));
                string unit = Request.Form["ud" + ud];


                Globals.Server.BuyUpgrade(Globals.CurrentLogin, ud, unit);
            }
        }
    }

    #endregion
}