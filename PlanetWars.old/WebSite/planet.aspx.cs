#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

using PlanetWarsShared;

#endregion

public partial class PlanetPage : CommonPageBase
{
    Player planetOwner;
    Planet displayedPlanet;

    protected void Page_Init(object sneder, EventArgs e)
    {
        string pname = Request["name"];
        displayedPlanet = Globals.Galaxy.Planets.SingleOrDefault(p => p.Name == pname);
    }
    
    protected void Page_Load(object sender, EventArgs e)
    {

        if (!IsPostBack) {
            string pname = Request["name"];
            lbName.Text = pname;
            displayedPlanet = Globals.Galaxy.Planets.SingleOrDefault(p => p.Name == pname);
            if (displayedPlanet == null) return;

            lbName.Text += HtmlRenderingExtensions.GetFactionImage(displayedPlanet.FactionName);
            imgMap.ImageUrl = HtmlRenderingExtensions.GetMinimapUrl(displayedPlanet.MapName);
            if (Globals.Player != null && Globals.Player.Name == displayedPlanet.OwnerName) {
                lbRename.Visible = true;
                tbName.Visible = true;
                tbName.Text = displayedPlanet.Name;
                btnSubmit.Visible = true;

                if (Globals.Player.HasChangedMap) {
                    ddMap.Visible = false;
                } else {
                    ddMap.Visible = true;
                    var items = Globals.Galaxy.GetAvailableMaps().OrderBy(x => x);
                    foreach (var c in items) {
                        ddMap.Items.Add(c);
                    }
                    ddMap.SelectedValue = displayedPlanet.MapName;
                }
            }

            if (Globals.Player != null) {
                var fleet = Globals.Galaxy.Fleets.SingleOrDefault(f => f.OwnerName == Globals.MyPlayerName);
                if (fleet != null) 
                {
                    btnSendFleet.Visible = true;
                    btnSendFleet.Text = string.Format("Send blockade fleet to {0} ({1} turns)", displayedPlanet.Name, fleet.EtaToPlanet(displayedPlanet.ID, Globals.Galaxy));
               }
            }


            // events
            var sb = new StringBuilder();
            Globals.Galaxy.Events.Where(
                x => x.IsPlanetRelated(displayedPlanet.ID) && !x.IsHiddenFrom(Globals.MyFaction)).
                Reverse().ForEach(
                x => sb.AppendFormat("{0}: {1}<br/>\n", x.TimeOffset(Globals.Player), x.ToHtml()));

            litEvents.Text = sb.ToString();
                

            lbFaction.Text = HtmlRenderingExtensions.GetFactionLink(displayedPlanet.FactionName);
            planetOwner = Globals.Galaxy.GetOwner(displayedPlanet.ID);
            if (planetOwner != null) {
                bool isOwner = false;
                if (Globals.Player != null)
                {
                    if (Globals.Player.Name == planetOwner.Name) isOwner = true;
                }


                lbOwner.Text = planetOwner.ToHtml();
                if (planetOwner.FactionName != displayedPlanet.FactionName) {
                    lbFaction.Text += " <span style='color:red;'>(Occupied)</span>";
                }

                if (isOwner) {
                    tbDesciption.Text = planetOwner.Description + "";
                    tbDesciption.Visible = true;
                    btnSetDesciption.Visible = true;
                }

                DisplayPlanetStructures(displayedPlanet);
            } else {
                lbOwner.Text = "uncolonized";
            }

            litPlanetInfo.Text = GetMapInfoTable(displayedPlanet.MapName, planetOwner != null ? planetOwner.Description : null);
            litPlanetInfo.Text += string.Format("<a href='heightmaps/{0}.jpg' target='_new'>Topological scan</a><br/><a target='_new' href='metalmaps/{0}.png'>Mineral scan</a>", displayedPlanet.MapName);
        }
    }

    void DisplayPlanetStructures(Planet planet)
    {
        if (planet == null) return;
        var sb = new StringBuilder();
        var mi = Utils.GetMapInfoCached(planet.MapName);
        var imgDim = Utils.GetImageDimensionsCached(HtmlRenderingExtensions.GetMinimapUrl(planet.MapName));

        if (Globals.Server.UpgradeData == null) return;


        // array with coordiantes
        var coords = new StringBuilder();
        coords.Append("<script type='text/javascript'>\nvar tpos = Array(");

        var js = new StringBuilder();
        js.Append("<script type='text/javascript'>\nfunction AddHovers() {\n");


        int cnt = 0;        
        foreach (var up in Globals.Server.UpgradeData) {
            bool isAlly = Globals.Galaxy.GetPlayer(up.Key).FactionName == Globals.MyFaction;
            bool isSelf = up.Key == Globals.MyPlayerName;
            foreach (var upd in up.Value.Where(x=> x.QuantityDeployed > 0 && x.IsBuilding)) foreach (var dep in upd.DeployLocations.Where(x=> x.PlanetID == planet.ID)) {

                string icon = "static_enemy";
                if (isSelf) icon = "static_self";
                else if (isAlly) icon = "static_ally";
                
                // images
                 sb.AppendFormat(
                        "<img src='uniticons/{1}.png' style='position:absolute;z-index:2;top:0;left:0;' id='img{0}'/></a>",
                        cnt, icon);


                // array with coordiantes
                if (cnt > 0) {
                    coords.Append(",");
                }
                int posx = (int)(((double)dep.X/mi.Width)*imgDim.X) - 5;
                int posy = (int)(((double)dep.Z/mi.Height)*imgDim.Y) - 5;
                coords.AppendFormat("{0},{1}", posx, posy);

                // js tooltips
                string htmltext =
                    String.Format(
                        "<div class=\"popup\"><img src=\"unitpics/{0}.png\"><br/>{1}<br/>",
                        upd.UnitChoice,
                        up.Key);
                js.AppendFormat("PW.AddTooltip('img{0}', '{1}', null);\n", cnt, htmltext);


                cnt++;
            }
        }
        


        coords.Append(");\n");
        coords.AppendFormat("var firefoxFix = {0};", imgDim.Y);
        coords.Append("</script>\n");

        
        litPlanetStructures.Text = sb.ToString() + coords.ToString();

        js.Append("\n}\naddLoadEvent(AddHovers);\n");
        js.Append("</script>\n");
        litPopupLinks.Text = js.ToString();
    }

    public static string GetMapInfoTable(string mapName, string description)
    {
        if (string.IsNullOrEmpty(mapName)) {
            return "";
        }
        var sb = new StringBuilder();

        var mapInfo = Utils.GetMapInfoCached(mapName);
        
        if (description != null) {
            var sw = new StringWriter();
            HttpContext.Current.Server.HtmlEncode(description, sw);
            sb.Append(sw.ToString().Replace("\n", "<br/>").Replace("\r", "").Replace("'","`"));
        }
        
        sb.Append("<table style=\"border:0;\">");
        sb.AppendFormat(
            "<tr><td>Scan dimensions:</td><td>{0} x {1} km</td></tr>",
            mapInfo.Width/512,
            mapInfo.Height/512);
        sb.AppendFormat("<tr><td>Gravity:</td><td>{0:f1} g</td></tr>", (double)mapInfo.Gravity/100.0);
        sb.AppendFormat(
            "<tr><td>Wind:</td><td>{0} - {1} w/m^2</td></tr>", mapInfo.MinWind, mapInfo.MaxWind);
        sb.AppendFormat(
            "<tr><td>Tidal strength:</td><td>{0} w/m^2</td></tr>", mapInfo.TidalStrength);
        sb.AppendFormat("<tr><td>Mineral abundance:</td><td>{0:f2}</td></tr>", mapInfo.MaxMetal);
        if (!string.IsNullOrEmpty(mapInfo.Author)) sb.AppendFormat("<tr><td>Discovered by:</td><td>{0}</td></tr>", mapInfo.Author);
        sb.AppendFormat("</table>");

        return sb.ToString();
    }

    protected void btnSubmit_Click(object sender, EventArgs e)
    {
        string planetMessage;
        string mapMessage;
        bool planetSuccess = Globals.Server.ChangePlanetName(
            tbName.Text, Globals.CurrentLogin, out planetMessage);
        bool mapSuccess = Globals.Server.ChangePlanetMap(
            ddMap.SelectedValue, Globals.CurrentLogin, out mapMessage);

        if (!planetSuccess || !mapSuccess) {
            MessageBox.Show(
                (planetSuccess ? planetMessage : String.Empty) +
                (!planetSuccess && !mapSuccess ? Environment.NewLine : String.Empty) +
                (mapSuccess ? mapMessage : String.Empty));
            return;
        }
        lbName.Text = tbName.Text;
        ddMap.Visible = false;
    }

    protected void ddMap_SelectedIndexChanged(object sender, EventArgs e)
    {
        imgMap.ImageUrl = HtmlRenderingExtensions.GetMinimapUrl(ddMap.SelectedValue);
        litPlanetInfo.Text = GetMapInfoTable(ddMap.SelectedValue, planetOwner != null ? planetOwner.Description : null);
    }
    protected void btnSetDesciption_Click(object sender, EventArgs e)
    {
        string mes;
        if (!Globals.Server.ChangePlanetDescription(tbDesciption.Text, Globals.CurrentLogin, out mes)) MessageBox.Show(mes);
        else {
            litPlanetInfo.Text = GetMapInfoTable(Globals.Galaxy.GetPlanet(Globals.Player.Name).MapName, tbDesciption.Text);
        }
    }
    protected void btnSendFleet_Click(object sender, EventArgs e)
    {
        string message;
        if (!Globals.Server.SendBlockadeFleet(Globals.CurrentLogin, displayedPlanet.ID, out message))
            MessageBox.Show(message);
    }
}