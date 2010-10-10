using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class FactionPage : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
        string factName = Request["name"];
        if (!IsPostBack) {
		    Label1.Text = factName + " commanders";
		}

	    const int MaxLines = 100;

        int shift = 0;
        int.TryParse(Request["shift"], out shift);

	    int maxim = Globals.Galaxy.Players.Where(x => x.FactionName == factName).Count();

        string nline = HtmlRenderingExtensions.GetPrevNext(maxim, shift, MaxLines, "name=" + factName + "&");
        var sb = new StringBuilder();

        sb.Append("<table style='border:0px solid;' border='0' cellspacing='0' cellpadding=0'>\n");
        sb.Append(nline);
	    sb.AppendLine(
            "<tr><th>Name</th><th>Rank points (wins-defeats)&nbsp;</th><th>Credits (used)&nbsp;</th><th>Credits sent - recieved&nbsp;</th><th>Clout</th><th>Skill (1v1)</th></tr>\n");
	    int cnt = 0;
        foreach (var p in Globals.Galaxy.Players.Where(x => x.FactionName == factName).OrderBy(x => x.RankOrder).Skip(shift).Take(MaxLines))
        {
            sb.AppendFormat(
                "<tr><td><span style='color:aqua'/>{11}. </span>{0}</td><td>{1:F1} ({2}-{3})</td><td>{4} ({5})</td><td>{6} - {7}</td><td>{8}</td><td>{9:f0} ({10:f0})</td></tr>\n", p.ToHtml(), p.RankPoints, p.Victories, p.Defeats, p.MetalAvail, p.MetalSpent, p.SentMetal, p.RecievedMetal, p.Clout,p.Elo, p.Elo1v1, 1+ cnt++);
        }
        sb.Append(nline);

        sb.Append("</table>");
	    litPlayers.Text = sb.ToString();

	}
}
