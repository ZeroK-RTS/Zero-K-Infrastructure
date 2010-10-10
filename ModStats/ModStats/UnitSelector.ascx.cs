using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModStats
{
	public partial class UnitSelector : System.Web.UI.UserControl
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void LinqDataSource1_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			e.Result = from u in Global.Db.Units
					   group u by u.Unit1
						   into g orderby g.Key
						   select new { g.Key };
		}

		protected void btn_Click(object sender, EventArgs e)
		{
			foreach (var i in lbSource.GetSelectedIndices()) {
				var item = lbSource.Items[i];
				if (!lbSelection.Items.Contains(item)) lbSelection.Items.Add(item);
			}
		}

		protected void btnRemove_Click(object sender, EventArgs e)
		{
			var todel = new List<ListItem>();
			foreach (var i in lbSelection.GetSelectedIndices())
			{
				todel.Add(lbSelection.Items[i]);
			}
			foreach (var i in todel) lbSelection.Items.Remove(i);

		}

		public List<string> GetSelectedUnits()
		{
			var ret = new List<string>();

			foreach (var x in lbSelection.GetSelectedIndices())
			{
				ret.Add(lbSelection.Items[x].Value);
			}
			return ret;
		}
	}
}