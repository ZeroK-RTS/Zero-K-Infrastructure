using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModelBase
{
	public partial class SubversionHowTo : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (Global.LoggedUser != null) {
				lbSvn.Text = "svn://springrts.com/modelbase/" + Global.LoggedUser.Login;
			}
		}
	}
}
