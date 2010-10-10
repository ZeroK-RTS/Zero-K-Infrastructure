using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PlanetWarsShared.Events;

public partial class EventsPage : System.Web.UI.Page
{
	protected void Page_Load(object sender, EventArgs e)
	{
	    const int MaxLines = 50;
        
	    int shift = 0;
	    int.TryParse(Request["shift"], out shift);

	    string nline = HtmlRenderingExtensions.GetPrevNext(int.MaxValue, shift, MaxLines);
        var sb = new StringBuilder();
        //typeof(Event)
	    var allowedTypes = new List<Type>();
        foreach (var t in Assembly.GetAssembly(typeof(Event)).GetTypes()) {
            if (t.IsSubclassOf(typeof (Event))) {
                string nam = t.Name.Remove(t.Name.Length-5,5);
				bool blocked = Request[nam] != null;
                sb.AppendFormat(
                    "<input type='checkbox' name='{0}' value='{0}' {1}>{0}&nbsp;&nbsp;",
                    nam,
                     blocked ? "checked" : "");
                if (blocked) allowedTypes.Add(t);
            }
        }
	    sb.Append("<input type='submit' value='Filter'/>");

	    sb.Append("<table style='border:0'>\n");
        sb.Append(nline);

        var p = Globals.Player;
        foreach (var ev in Globals.Galaxy.Events.Where(x => allowedTypes.Count == 0 || allowedTypes.Contains(x.GetType()) && !x.IsHiddenFrom(Globals.MyFaction)).Reverse().Skip(shift).Take(MaxLines)) {
            sb.AppendFormat(
                "<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>\n", ev.TimeOffset(p), ev.Turn, ev.ToHtml());
	    }
	    sb.Append(nline);
        
	    sb.Append("</table>");

	    litEvents.Text = sb.ToString();
        litEvents.EnableViewState = false;

	}
}
