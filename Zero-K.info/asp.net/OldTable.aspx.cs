using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZkData;

namespace MissionEditorServer
{
	public partial class Default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack) GridView1.Sort("ModifiedTime", SortDirection.Descending);

			/*var db = EditorService.GetContext();
			var best = from m in db.Missions
			             from s in m.Scores where s.Score1 >= m.Scores.Max(x=>x.Score1)
			             select new {Mis = m, Sc = s};

			foreach (var ret in best) {
				ret.Mis.TopScoreLine = string.Format("{0} ({1} in {2})", ret.Sc.PlayerName, ret.Sc.Score1, EditorService.SecondsToTime(ret.Sc.TimeSeconds));
			}

			db.SubmitChanges();*/

		}

		protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
		{
			var db = new ZkDataContext();
			if (e.CommandName == "download") {
				var key = Convert.ToInt32(e.CommandArgument);
				Response.Clear();
				Response.Buffer = true;
				Response.ContentEncoding = Encoding.UTF8;
				Response.ContentType = "file/sdz";
				var prev = db.Missions.Where(x => x.MissionID == key).Single();
				var name = prev.Name;
				foreach (var s in Path.GetInvalidFileNameChars()) name.Replace(s, '_');
				name = name.Replace(":", "_");
				Response.AppendHeader("content-disposition", "inline; filename=" + name + ".sdz");


                var data = prev.Mutator;
				prev.DownloadCount++;
				db.SubmitChanges();
				Response.OutputStream.Write(data.ToArray(), 0, data.Length);
				Response.End();

			} else if (e.CommandName =="comments") {
				var key = Convert.ToInt32(e.CommandArgument);
/*				var items = db.Comments.Where(x => x.MissionID == key).OrderBy(x => x.Time);
				Label1.Text = "";
				foreach (var i in items) {
					Label1.Text += string.Format("<hr/>{0}<br/>{1}<br/>{2}<br/>", i.Time.ToLocalTime(), i.Nick, i.Text);
				}*/

			} else if (e.CommandName=="top10") {
				/*var key = Convert.ToInt32(e.CommandArgument);
				var db = EditorService.GetContext();
				Label1.Text = string.Format("Scoring rules: {0}<br/><h3>Top players:</h3>", (from m in db.Missions where m.MissionID == key select m.ScoringMethod).Single());

				var items = db.Scores.Where(x => x.MissionID == key).OrderByDescending(x => x.Score1);
				int cnt = 1;
				foreach (var i in items) {

					Label1.Text += string.Format("<b>{0}. {1}</b>  {2}  (in {3})</br>\n",cnt, i.PlayerName.PadLeft(15), i.Score1.ToString().PadLeft(5), EditorService.SecondsToTime(i.TimeSeconds));
					cnt++;
				}*/

			}
		}

		protected void LinqDataSource1_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			var db = new ZkDataContext();
			e.Result = db.Missions.Select(x => new {x.Name, x.Description, Author=x.Account.Name, x.CreatedTime, x.DownloadCount, x.Map, x.Mod, x.ModifiedTime, x.MissionID, x.TopScoreLine});
		}

	}
}
