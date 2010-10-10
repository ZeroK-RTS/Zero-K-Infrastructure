using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PlanetWarsShared;

public partial class chat : System.Web.UI.Page
{
    const int DisplayLines = 80;
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Globals.Player != null) {
            var fact = Globals.Galaxy.GetFaction(Globals.MyFaction);

            int shift = 0;
            int.TryParse(Request["shift"], out shift);
            int cnt = 0;
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("<h2>#{0} channel chat history</h2>", Globals.MyFaction);

            string pnline = HtmlRenderingExtensions.GetPrevNext(fact.ChatEvents.Count, shift, DisplayLines);

            sb.Append("<table style='border:0; ' cellspacing='0' cellpadding='0' border='0'>");
            sb.Append(pnline);

            for (int i = Math.Max(0, fact.ChatEvents.Count - DisplayLines - shift); i < fact.ChatEvents.Count && cnt < DisplayLines; i++, cnt++)
            {
                var c = fact.ChatEvents[i];
                sb.AppendFormat("<tr><td>{0}</td><td>&lt;<span style='color:aqua;'>{1}</span>&gt;</td><td>&nbsp;{2}</td></tr>", c.Time, Server.HtmlEncode(c.Name), Server.HtmlEncode(c.Text));
            }
            sb.Append(pnline);
            sb.Append("</table>");

            litChat.Text = sb.ToString();

        }


    }
}
