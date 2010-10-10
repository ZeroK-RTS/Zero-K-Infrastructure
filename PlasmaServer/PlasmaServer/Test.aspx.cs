using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PlasmaServer
{
	public partial class Test : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			/*var db = new DbDataContext();
			foreach (var r in db.Resources) {
				var dupLens = from rf in r.ResourceContentFiles
				          group rf by rf.Length
				          into g where g.Count() > 1 select g.Key;

				foreach (var len in dupLens) {
					foreach (var cf in r.ResourceContentFiles.Where(x=>x.Length == len)) {
						db.ResourceContentFiles.DeleteOnSubmit(cf);
					}
				}
			}
			db.SubmitChanges();*/

			PlasmaService ps = new PlasmaService();
			List<string> links;
			byte[] tor;
			List<string> dep;
			ResourceType type;
			string tfn;
			ps.DownloadFile("Complete Annihilation stable-6606", out links, out tor, out dep, out type, out tfn);
			foreach (var link in links) {
				Response.Write(link+"\n");
			}
		}
	}
}
