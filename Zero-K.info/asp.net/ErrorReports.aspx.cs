using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZkData;

namespace ZeroKWeb
{
	public partial class ErrorReports : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			ZeroKContext = new ZkDataContext();
		}

		DataContext ZeroKContext { get; set; }
	}
}