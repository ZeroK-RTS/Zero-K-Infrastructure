using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PlanetWarsShared;

/// <summary>
/// Summary description for HtmlRenderingExtensions
/// </summary>
public static class HtmlRenderingExtensions
{
	public static string ToHtml(this Player p)
	{
		if (p == null) return "";
        return String.Format("<img src=\"{1}\" style=\"height:20px;\"><a href=\"player.aspx?name={0}\">{0}</a>{2}", p.Name, p.Rank.GetImageUrl(), GetFactionImage(p.FactionName));
	}

    public static string ToHtml(this Planet p)
    {
        if (p == null) return "";
        return String.Format("<a href=\"planet.aspx?name={0}\">{1}</a>{2}", Uri.EscapeDataString(p.Name), p.Name, GetFactionImage(p.FactionName));
    }

    public static string GetFactionLink(string factionName)
    {
        return String.Format("<a href=\"faction.aspx?name={0}\">{0}</a>{1}", factionName, GetFactionImage(factionName));
    }


	public static string GetImageUrl(this Rank r)
	{
		return String.Format("rankicons/{0:000}.png", (int)r);
	}

    public static string GetMinimapUrl(string mapName)
    {
        if (String.IsNullOrEmpty(mapName))
        {
            return "factions/neutral.png";
        }
        else return String.Format("minimaps/{0}.jpg", mapName);
    }

    public static string GetFactionImage(string factionName)
    {
        return String.Format("<img src=\"factions/{0}.png\">", factionName ?? "neutral");
    }

    public static string GetPrevNext(int count, int shift, int maxLines)
    {
        return GetPrevNext(count, shift, maxLines, "");
    }

    public static string GetPrevNext(int count, int shift, int maxLines, string extraParams) {
        string next = "";
        string prev = "";
        if (count - shift - maxLines> 0)
            next = String.Format(
                "<a href=\"?{1}shift={0}\" style=\"font-size:larger;\">Next</a>", shift + maxLines, extraParams);
        if (shift > 0) prev = String.Format(
            "<a href=\"?{1}shift={0}\" style=\"font-size:larger;\">Previous</a>", Math.Max(0, shift - maxLines), extraParams);


        return String.Format(
            "<tr><td colspan=\"2\" align=\"left\">{0}</td><td colspan=\"10\" align=\"right\">{1}</td></tr>",
            prev,
            next);
    }
}
