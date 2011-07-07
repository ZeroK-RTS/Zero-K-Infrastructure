using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModelBase
{
	public partial class ModelsForm : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void btnSearch_Click(object sender, EventArgs e)
		{
			int cnt;
			int.TryParse(tbResults.Text, out cnt);
			GridView1.PageSize = cnt;
			GridView1.DataBind();
		}


		protected void gridDataSource_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			int authorID;
			int.TryParse(ddAuthor.Text, out authorID);
			var ret = from m in Global.Db.Models
			          where !m.IsDeleted && (authorID == 0 || authorID == m.AuthorUserID) select m;
            
			if (!string.IsNullOrEmpty(tbName.Text)) {
				ret = ret.Where(x => x.Name.Contains(tbName.Text));
			}

			if (!string.IsNullOrEmpty(tbDescription.Text)) {
				ret = ret.Where(x => x.Description.Contains(tbDescription.Text));
			}

			if (ddTags.SelectedItem != null) {
				foreach (ListItem item in ddTags.Items) {
					int val = int.Parse(item.Value);
					if (item.Selected) ret = ret.Where(x => x.ModelTags.Any(y=>y.TagID == val));

				}
			}
			e.Result = ret.OrderByDescending(m=>m.Modified).Select(m=>new {m.Name, m.ModelID, TagString = Global.PrintTags(m.ModelTags), m.Modified, UserName= m.User.Login, m.Description, m.ModelProgress, m.TextureProgress, m.ScriptProgress, m.OverallProgress, IconUrl = m.GetIconUrl()});
		}

		protected void btnRefresh_Click(object sender, EventArgs e)
		{
			Application["LastSvnUpdate"] = DateTime.UtcNow;
			new SvnController().Update();
			new ForumController().MakeModelPosts();
			new ForumController().MakeNewsPosts();
			GridView1.DataBind();
		}
	}
}
