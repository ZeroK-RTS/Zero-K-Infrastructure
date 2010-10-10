#region using

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using PlanetWarsShared;
using PlanetWarsShared.Events;
using PlanetWarsShared.Springie;

#endregion

public partial class IndexMapPage : CommonPageBase
{
    #region Constants

    protected const int leftBarSizeX = 200;
	protected const int topBarSizeY = 150;

    protected const int linkImagePadding = 10;
    protected const string mapFolderName = "links";

    // size of galaxy/galaxy.jpg
    protected const int mapSizeX = 3453;
    protected const int mapSizeY = 1764;

    #endregion

    #region Other methods

    int fleetId;

    void GeneratePlanetLinks(StringBuilder t)
    {
        var mapSize = new Size(mapSizeX, mapSizeY);
        foreach (var link in  Globals.Galaxy.Links) {
            string imageName = mapFolderName + "/" + link.GetFileName(Globals.Galaxy);

            if (!File.Exists(Server.MapPath(imageName))) {
                continue;
            }

            var planetPositions = Globals.Galaxy.GetPlanets(link).Select(p => p.Position).ToArray();
            planetPositions = planetPositions.Select(p => p.Scale(mapSize)).ToArray();
            // to image coords
            var imageRectangle = planetPositions.ToRectangleF().ToRectangle().PadRectangle(linkImagePadding);

            int x = imageRectangle.X + leftBarSizeX;
            int y = imageRectangle.Y + topBarSizeY;

            string html =
                "<div style='position:absolute; z-index:1; left:{0}px; top:{1}px; color:white;'><img src='{2}' style='float:left;'></div>";
            t.Append(String.Format(html, x, y, imageName));
        }
    }

    int GetPlanetX(Planet p)
    {
        return ToScreenX(p.Position.X);
    }

    public int GetPlanetY(Planet p)
    {
        return ToScreenY(p.Position.Y);
    }

    int ToScreenX(double x)
    {
        return (int)(x*mapSizeX + leftBarSizeX);
    }

    int ToScreenY(double y)
    {
        return (int)(y*mapSizeY + topBarSizeY);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        var js = new StringBuilder();
        var t = new StringBuilder();
        GeneratePlanetLinks(t);

        js.Append("PW.init();\n");
        js.Append("PW.graphics.SetStroke(1);\n");
        js.Append("PW.graphics.SetColor('#89fecd')\n");

        var springieStates = Globals.Server.GetSpringieStates();

        var hide = Globals.Galaxy.Planets.Where(x=>x.FactionName==null).Select(x=>x.ID);
        if (hide.Count() > 0) hide = hide.Except(Globals.Galaxy.GetClaimablePlanets().Select(x => x.ID));

        var planetAttacked = new Dictionary<int, string>();
		int cnt = 0;
        foreach (var state in springieStates) {
            int num = cnt%3;
            if (state.Value.ReminderEvent == ReminderEvent.OnBattleStarted) {
                planetAttacked[state.Value.GameStartedStatus.PlanetID] = "img/planetstatus/attacking" + num + ".png";
            } else {
                planetAttacked[state.Value.PlanetID] = "img/planetstatus/preparing" + num + ".png";
            }
			cnt++;
        }

        var planetBattles = new Dictionary<int, IEnumerable<BattleEvent>>();
        foreach (var b in Globals.Galaxy.Events.OfType<BattleEvent>().GroupBy(x => x.PlanetID)) {
            planetBattles.Add(b.Key, b);
        }
		


 
        var planetOrbits = new Dictionary<int, List<SpaceFleet>>();
        var fleetIcon = Utils.GetImageDimensionsCached("img/fleets/Arm.png");
        foreach (var fleet in Globals.Galaxy.Fleets) {
            PointF curpos;
            if (fleet.GetCurrentPosition(out curpos, Globals.Galaxy.Turn)) {
                List<SpaceFleet> fleets;
                if (!planetOrbits.TryGetValue(fleet.TargetPlanetID, out fleets)) {
                    fleets = new List<SpaceFleet>();
                }
                fleets.Add(fleet);
                planetOrbits[fleet.TargetPlanetID] = fleets;
            } else {
                RenderFleet(fleet, ToScreenX(curpos.X) - fleetIcon.X/2, ToScreenY(curpos.Y) - fleetIcon.Y/2, t, js);
            }
        }

        foreach (var planet in Globals.Galaxy.Planets) {
            if (hide.Contains(planet.ID)) continue;
            bool isMy = (Globals.Player != null && planet.OwnerName == Globals.Player.Name);

            var player = planet.OwnerName != null ? Globals.Galaxy.GetPlayer(planet.OwnerName) : null;

            string planetImage = string.Format("planets/{0}.png", player == null ? "neutral" : player.FactionName.ToLower());
            // planet.ID % 15

            var imgSize = Utils.GetImageDimensionsCached(planetImage);


            string divid = "p" + planet.ID;


            // currently selected marker
            string overlay;
            if (planetAttacked.TryGetValue(planet.ID, out overlay)) {
                t.AppendFormat(
                    "<a href='planet.aspx?name={5}' id='{6}'><img src='{4}' style='position:absolute; z-index:3; left:{0}px; top: {1}px;width:{2}px; height:{3}px'></a>",
                    GetPlanetX(planet) - (imgSize.X + 10)/2,
                    GetPlanetY(planet) - (imgSize.Y + 10)/2,
                    imgSize.X + 10,
                    imgSize.Y + 10,
                    overlay,
                    Uri.EscapeDataString(planet.Name),
                    divid);
            }

            // main image + text
            t.AppendFormat(
                "<a href='planet.aspx?name={3}' id='{5}' class='mapplanet' style='left:{0}px;top:{1}px;{4}'><img src='{2}'></a>",
                GetPlanetX(planet) - imgSize.X/2,
                GetPlanetY(planet) - imgSize.Y/2,
                planetImage,
                Uri.EscapeDataString(planet.Name),
                isMy ? "text-decoration: overline;" : "", overlay == null ?divid :"");

            t.AppendFormat(
                "<span class='mapowner' style='left:{0}px;top:{1}px;{3}'>{2}</span>",
                GetPlanetX(planet) + imgSize.X/2,
                GetPlanetY(planet) - imgSize.Y/2 - 5,
                planet.ToHtml(),
                isMy ? "text-decoration: overline;" : "");

            t.AppendFormat(
                "<span class='mapowner' style='left:{0}px;top:{1}px;{3}'>{2}</span>",
                GetPlanetX(planet) + imgSize.X / 2,
                GetPlanetY(planet) - imgSize.Y / 2 + 10,
                player.ToHtml(),
                isMy ? "text-decoration: overline;" : "");



            // past battles
            IEnumerable<BattleEvent> battles;
            if (planetBattles.TryGetValue(planet.ID, out battles)) {
                int battleCount = battles.Count();
                var iconSize = Utils.GetImageDimensionsCached("img/battles/Arm.png");
                const int maxLine = 5;
                int offsetY = -imgSize.Y/2 - (((battleCount - 1)/maxLine) + 1)*iconSize.Y;
                int offsetX = -iconSize.X*Math.Min(maxLine, battleCount)/2;

                int i = 0;
                foreach (var battle in battles) {
                    var factionPlayers = from p in battle.EndGameInfos
                                         where !p.Spectator
                                         group p by p.Side
                                             into grouped
                                             select new { factionName = grouped.Key, factionCount = grouped.Count() };

                    string sideStrength = "";

                    foreach (var fp in factionPlayers)
                    {
                        sideStrength += string.Format("{0} {1} armies<br/>", fp.factionName, fp.factionCount);
                    }


                    string iconName = string.Format(
                        "img/battles/{0}{1}.png", battle.Victor, battle.Attacker == battle.Victor ? "" : "def");
                    t.AppendFormat(
                        "<a href='battle.aspx?turn={0}' id='b{0}'><img id='b{0}' src='{3}' style='position:absolute;z-index:4;left:{1}px;top:{2}px;'></a>",
                        battle.Turn,
                        GetPlanetX(planet) + offsetX + (i%maxLine)*iconSize.X,
                        GetPlanetY(planet) + offsetY + (i/maxLine)*iconSize.Y, iconName);
                    js.AppendFormat(
                        "PW.AddTooltip('b{0}', '<div class=\"popup\">{1} The {2} have {3} {4}.<br/>{5}</div>', null);\n",
                        battle.Turn,
                        HtmlRenderingExtensions.GetFactionImage(battle.Victor),
                        battle.Victor,
                        battle.Attacker == battle.Victor ? "invaded" : "protected",
                        planet.Name,
						sideStrength);
                    i++;
                }
            }

            List<SpaceFleet> fleets;
            if (planetOrbits.TryGetValue(planet.ID, out fleets)) {
                int count = fleets.Count;
                var iconSize = Utils.GetImageDimensionsCached("img/fleets/Arm.png");
                const int maxLine = 5;
                int offsetY = imgSize.Y/2;
                int offsetX = -iconSize.X*Math.Min(maxLine, count)/2;

                int i = 0;
                foreach (var fleet in fleets) {
                    RenderFleet(
                        fleet,
                        GetPlanetX(planet) + offsetX + (i%maxLine)*iconSize.X,
                        GetPlanetY(planet) + offsetY + (i/maxLine)*iconSize.Y,
                        t,
                        js);
                    i++;
                }
            }

            string htmltext =
                String.Format(
                    "<div class=\"popup\"><b>Name:</b> {0}<br /><b>Owner:</b> {1}<br /><b>Map:</b> {2}<br/><img src=\"{3}\" style=\"max-width: 200px\"><br/>{4}</div>",
                    planet.Name,
                    planet.OwnerName ?? "Unknown",
                    Path.GetFileNameWithoutExtension(planet.MapName ?? "Uncharted"),
                    HtmlRenderingExtensions.GetMinimapUrl(planet.MapName),
                    PlanetPage.GetMapInfoTable(planet.MapName, player != null ? player.Description : null));

            js.AppendFormat("PW.AddTooltip('{0}', '{1}', {2});\n", divid, htmltext, "null");
        }

        js.Append("PW.graphics.Paint();\n");
        litPlanets.Text += t.ToString();
        litJs.Text = "<script language=\"javascript\">\nfunction doStuff() {" + js +
                     "\n}\naddLoadEvent(doStuff);</script>";
    }

    void RenderFleet(SpaceFleet f, int x, int y, StringBuilder web, StringBuilder js)
    {
        fleetId++;
        string img = string.Format(
            "img/fleets/{0}{1}.png",
            Globals.Galaxy.GetPlayer(f.OwnerName).FactionName,
            Globals.Galaxy.Turn >= f.Arrives ? "_orbit" : "");

        web.AppendFormat(
            "<img id='f{0}' src='{3}' style='position:absolute;z-index:4;left:{1}px;top:{2}px;'>", fleetId, x, y, img);
        js.AppendFormat(
            "PW.AddTooltip('f{0}', '<div class=\"popup\">{1}</div>', null);\n",
            fleetId,
            f.GetHumanReadableEta(Globals.Galaxy).Replace("\n", "<br/>"));

        if (f.Arrives > Globals.Galaxy.Turn) {
            PointF curPos;
            f.GetCurrentPosition(out curPos, Globals.Galaxy.Turn);
            js.AppendFormat(
                "PW.graphics.DrawLine({0},{1},{2},{3}, 1);\n",
                ToScreenX(curPos.X),
                ToScreenY(curPos.Y),
                ToScreenX(f.Destination.X),
                ToScreenY(f.Destination.Y));
        }
    }

    #endregion
}
