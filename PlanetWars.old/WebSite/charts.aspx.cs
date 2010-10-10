using System;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using PWDataLib;

public partial class Charts : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {

        var sw = Stopwatch.StartNew();
        var galaxy = Globals.Galaxy;
        sw.Stop();
        Literal1.Text = "Fetch galaxy: " + sw.ElapsedMilliseconds + "ms<br>";
        sw = Stopwatch.StartNew();
        var serverChanged = (DateTime?)HttpContext.Current.Application["lastChanged"];
        var chartDate = (DateTime?)HttpContext.Current.Application["chartDate"];
        var charts = (string[])HttpContext.Current.Application["charts"];
        string[] pwCharts;
        if (!serverChanged.HasValue || charts == null || chartDate.Value.AddMinutes(5) < serverChanged) {
            pwCharts = new PWCharts(galaxy).Charts;
            HttpContext.Current.Application["charts"] = pwCharts;
        } else {
            pwCharts = charts;
        }
        HttpContext.Current.Application["chartDate"] = DateTime.Now;
        sw.Stop();
        Literal1.Text += "Compute data: " + sw.ElapsedMilliseconds + "ms<br>";
        Literal1.Text += String.Concat(pwCharts.Select(s => String.Format("<img src=\"{0}\"><br>", s)).ToArray());
    }
}