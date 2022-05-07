﻿using System;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Licho.Utils.Web;
using PlasmaShared;
using ZeroKWeb;
using ZkData;

namespace ZeroKWeb
{
	public partial class ResourceDetail : Page
	{
		#region Fields

		private int resourceID;

		#endregion

		#region Other methods

		protected void lqContentFiles_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			var db = new ZkDataContext();
			e.Result =
				db.ResourceContentFiles.Where(x => x.ResourceID == resourceID).ToList().Select(
					x =>
					new
						{
							x.FileName,
							x.Md5,
							x.Length,
              TorrentFileName = PlasmaServer.GetTorrentFileName(x),
							x.LinkCount,
							x.ResourceID,
							LinkText = x.Links != null ? string.Join("<br/>", x.Links.Split('\n').Select(y => string.Format("<a href='{0}'>{0}</a>", y)).ToArray()): null
						});
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			var db = new ZkDataContext();
			if (!int.TryParse(Request["resourceID"], out resourceID)) {
				resourceID = db.Resources.Single(x => x.InternalName == Request["name"]).ResourceID;
			}
			

			if (!IsPostBack) {
				var res = db.Resources.Where(x => x.ResourceID == resourceID).Single();
				lbDetails.Text = string.Format("Download count: {0}<br/>\nFailed downloads (no links): {1}<br/>\n", res.DownloadCount, res.NoLinkDownloadCount);
				lbName.Text = res.InternalName;
				litLinks.Text = string.Join("<br/>", res.ResourceDependencies.Select(x => x.NeedsInternalName).ToArray());

				string name = res.InternalName.EscapePath();
				var sb = new StringBuilder();
				if (res.TypeID == ZkData.ResourceType.Map) {
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
				var db = new ZkDataContext();
				var todel = db.ResourceContentFiles.Single(x => x.Md5 == ((ResourceContentFile)e.OriginalObject).Md5);
				Utils.SafeDelete(PlasmaServer.GetTorrentPath(todel));

				db.ResourceContentFiles.DeleteOnSubmit(todel);
			    db.SaveChanges();

			    MessageBox.Show("Deleted " + todel.FileName);

			}
			else
			{
				MessageBox.Show("Not logged in");
			}

		}
	}
}