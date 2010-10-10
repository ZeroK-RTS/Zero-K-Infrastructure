using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModelBase
{
	public partial class WebForm1 : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void LinqDataSource1_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			e.Result = from x in Global.Db.Events
			           orderby x.Time descending
			           select new {FullText = String.Format("{0} {1}{2}", x.SummaryLinked,Global.Linkify(x.Text), x.SvnLog != null ? "<br/>" + x.SvnLog : null), x.Time};
			
			
		}
	}
}
