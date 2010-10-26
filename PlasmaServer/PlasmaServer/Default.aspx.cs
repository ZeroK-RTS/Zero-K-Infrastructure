using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Licho.Utils.Web;
using ZkData;

namespace PlasmaServer
{
	public partial class Default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void lqResources_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			var db = new ZkDataContext();
			e.Result = db.Resources.Where(x => SqlMethods.Like(x.InternalName, "%" + tbName.Text + "%")).OrderByDescending(x=>x.DownloadCount).Select(x => new
			                                                                                                         	{
			                                                                                                         		x.ResourceID,
																																																									x.InternalName,
																																																									x.LastLinkCheck,
																																																									x.TypeID,
																																																									x.DownloadCount,
																																																									x.NoLinkDownloadCount,
																																																									InternalNameEscaped = x.InternalName.EscapePath(),

		});
		}

		protected void tbName_TextChanged(object sender, EventArgs e)
		{
			GridView1.DataBind();
		}

		protected void btnLoginClicked(object sender, EventArgs e)
		{
			if (PlasmaService.IsAdmin(tbLogin.Text, tbPassword.Text)) {
				Session["login"] = true;
				panelLogin.Visible = false;
			} else {
				lbError.Text = "Invalid login or password";
			}
		}

		protected void lqResources_Deleting(object sender, LinqDataSourceDeleteEventArgs e)
		{
			e.Cancel = true;
			if ((bool?)Session["login"] == true) {
				var db = new ZkDataContext();
				var todel = db.Resources.Single(x=>x.InternalName == ((Resource)e.OriginalObject).InternalName);
				todel.RemoveResourceFiles();

				
				db.Resources.DeleteOnSubmit(todel);
				db.SubmitChanges();
				
				MessageBox.Show("Deleted " +  todel.InternalName);
        
			} else {
				MessageBox.Show("Not logged in");
			}
		}
	}
}
