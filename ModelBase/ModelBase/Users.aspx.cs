using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModelBase
{
	public partial class Users : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack) {
				GridView1.AutoGenerateEditButton = (Global.LoggedUser != null && Global.LoggedUser.IsAdmin);
			}
		}

		protected void UsersUpdated(object sender, LinqDataSourceStatusEventArgs e)
		{
			new SvnConfigMaker().Generate();
		}
	}
}
