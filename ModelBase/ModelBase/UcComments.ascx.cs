using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModelBase
{
	public partial class UcComments : System.Web.UI.UserControl
	{
		public int? UnitID
		{
			get { return (int?) ViewState["UnitID"]; }
			set { ViewState["UnitID"] = value; }
		}

		public int? ModelID
		{
			get { return (int?) ViewState["ModelID"]; }
			set { ViewState["ModelID"] = value; }
		}


		public override void DataBind()
		{
			if (Global.LoggedUserID == null)
			{
				tbText.Visible = false;
				btnSubmit.Visible = false;
			}
			else
			{
				tbText.Visible = true;
				btnSubmit.Visible = true;
			}

			if (UnitID != null)
				lbTitle.Text = "Comments for unit " + (from x in Global.Db.Units
													   where x.UnitID == UnitID
													   select x.Name).First();

			if (ModelID != null)
			{
				var model = (from x in Global.Db.Models
							 where x.ModelID == ModelID
							 select x).First();
				lbTitle.Text = string.Format("Comments for {0}'s {1}", model.User.Login, model.Name);
			}

			base.DataBind();
		}


		protected void Page_Load(object sender, EventArgs e)
		{
		}

		protected void LinqDataSource1_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			e.Result = (from x in Global.Db.Comments
			           where Equals(x.ModelID, ModelID) && Equals(x.UnitID, UnitID)
			           // note hacked "Equals" needed for SQL null handling
			           select new {Name = x.User.Login, x.Time, Text = Global.Linkify(x.Text)}).ToList();

		}

		protected void btnSubmit_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(tbText.Text) && tbText.Text.Trim() != "") {

				var c = new Comment() {ModelID = ModelID, UnitID = UnitID, UserID = Global.LoggedUserID.Value, Time = DateTime.UtcNow, Text = tbText.Text};
				Global.Db.Comments.InsertOnSubmit(c);
				Global.Db.SubmitChanges();

				var evType = EventType.UnitCommented;
				if (ModelID != null) evType = EventType.ModelCommented;
				Global.AddEvent(evType, c.CommentID, UnitID, ModelID, c.Text);
				Global.Db.SubmitChanges();


				tbText.Text = "";
				Repeater1.DataBind();
			}
		}
	}
}