using System;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Licho.Utils.Web;

namespace PlasmaServer
{
	public partial class ResourceDetail : Page
	{
		#region Fields

		private int resourceID;

		#endregion

		#region Other methods

		protected void lqContentFiles_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			var db = new DbDataContext();
			e.Result =
				db.ResourceContentFiles.Where(x => x.ResourceID == resourceID).ToList().Select(
					x =>
					new
						{
							x.FileName,
							x.Md5,
							x.Length,
							TorrentFileName= x.GetTorrentFileName(),
							x.LinkCount,
							x.ResourceID,
							LinkText = x.Links != null ? string.Join("<br/>", x.Links.Split('\n').Select(y => string.Format("<a href='{0}'>{0}</a>", y)).ToArray()): null
						});
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			var db = new DbDataContext();
			if (!int.TryParse(Request["resourceID"], out resourceID)) {
				resourceID = db.Resources.Single(x => x.InternalName == Request["name"]).ResourceID;
			}
			

			if (!IsPostBack) {
				var res = db.Resources.Where(x => x.ResourceID == resourceID).Single();
				lbDetails.Text = string.Format("Download count: {0}<br/>\nFailed downloads (no links): {1}<br/>\n", res.DownloadCount, res.NoLinkDownloadCount);
				lbName.Text = res.InternalName;
				litLinks.Text = string.Join("<br/>", res.Dependencies.Select(x => x.NeedsInternalName).ToArray());
				litHashes.Text = string.Join("<br/>", res.ResourceSpringHashes.Select(x => string.Format("{0}: {1}", x.SpringVersion, x.SpringHash)).ToArray());

				string name = res.InternalName.EscapePath();
				var sb = new StringBuilder();
				if (res.TypeID == ResourceType.Map) {
					sb.AppendFormat("<img src='Resources/{0}.minimap.jpg'><br/>", name);
					sb.AppendFormat("<img src='Resources/{0}.heightmap.jpg'><br/>", name);
					sb.AppendFormat("<img src='Resources/{0}.metalmap.jpg'><br/>", name);
				}
				sb.AppendFormat("<a href='Resources/{0}.metadata.xml.gz'>metadata</a><br/>", name);
				litBasics.Text = sb.ToString();
			}
		}

		#endregion

		protected void lqContentFiles_Deleting(object sender, LinqDataSourceDeleteEventArgs e)
		{
			e.Cancel = true;
			if ((bool?)Session["login"] == true)
			{
				var db = new DbDataContext();
				var todel = db.ResourceContentFiles.Single(x => x.Md5 == ((ResourceContentFile)e.OriginalObject).Md5);
				Utils.SafeDelete(todel.GetTorrentPath());

				db.ResourceContentFiles.DeleteOnSubmit(todel);
				db.SubmitChanges();

				MessageBox.Show("Deleted " + todel.FileName);

			}
			else
			{
				MessageBox.Show("Not logged in");
			}

		}
	}
}