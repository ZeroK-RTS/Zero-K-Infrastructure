using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModelBase
{
	public partial class Reload : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			new SvnController().Update();

			DateTime? lastRun = (DateTime?) Application["LastRun"];
			if (lastRun == null || lastRun.Value.DayOfYear != DateTime.UtcNow.DayOfYear) {
                new ForumController().MakeModelPosts();
				new ForumController().MakeNewsPosts();
				Application["LastRun"] = DateTime.UtcNow;
			}
		}
	}
}
