using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModelBase
{
	public partial class WebForm2 : System.Web.UI.Page
	{
		protected int GameID
		{
			get
			{
				if (string.IsNullOrEmpty(ddGame.SelectedValue)) return Global.Db.Games.First().GameID;
				else return int.Parse(ddGame.SelectedValue);
			}
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			var avg = Global.Db.Units.Where(u=>u.GameID == GameID).Average(u => (int?)u.OverallProgress);
			var total = Global.Db.Units.Where(u => u.GameID == GameID).Count();
			var cavedog = Global.Db.Units.Where(u => u.GameID == GameID).Count(u => u.LicenseType == 0);

			lbTotal.Text = string.Format("{4} progress:  {0:0.}%   ({1} models out of {2} ({3:0.}%) are still Cavedog)", avg, cavedog, total, 100.0 * cavedog / total, Global.Db.Games.Where(x=>x.GameID==GameID).Select(x=>x.Name).First());
		}

		protected void LinqDataSource1_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			IQueryable<Unit> r = Global.Db.Units.Where(u => u.GameID == GameID);
			if (!string.IsNullOrEmpty(tbCode.Text)) r = r.Where(u => u.Code == tbCode.Text);
			if (!string.IsNullOrEmpty(tbName.Text)) r = r.Where(u => u.Name.Contains(tbName.Text));
			if (!string.IsNullOrEmpty(tbDescription.Text)) r = r.Where(u => u.Description.Contains(tbDescription.Text));
			if (!string.IsNullOrEmpty(tbParent.Text)) r = r.Where(u => u.ParentCode == tbParent.Text);
			if (!string.IsNullOrEmpty(ddLicense.SelectedValue)) r = r.Where(u => u.LicenseType == int.Parse(ddLicense.SelectedValue));

			r = r.OrderBy(x => x.OverallProgress);

			e.Result = from x in r select new
			           		{
			           			x.UnitID,
			           			x.Name,
			           			x.Code,
			           			x.CurrentStatus,
			           			x.LicenseType,
			           			x.ModelProgress,
			           			x.TextureProgress,
			           			x.ScriptProgress,
			           			x.OverallProgress,
			           			LastChanged = x.User.Login,
								CandidateCount = x.Candidates.Count
			           		};
		}

		protected void Button1_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(tbResults.Text)) {
				GridView1.AllowPaging = true;
				GridView1.PageSize = int.Parse(tbResults.Text);
			} else {
				GridView1.AllowPaging = false;
			}
			GridView1.DataBind();
		}


		protected void ddGame_SelectedIndexChanged(object sender, EventArgs e)
		{
			GridView1.DataBind();
		}


	}
}
