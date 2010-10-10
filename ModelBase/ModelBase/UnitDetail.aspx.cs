using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ModelBase
{
	public partial class UnitDetailForm : System.Web.UI.Page
	{
		protected int UnitID
		{
			get
			{
				return int.Parse(Request["UnitID"]);
			}
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack) {
				UcComments1.UnitID = UnitID;
				UcComments1.DataBind();

				var unit = Global.Db.Units.Single(x => x.UnitID == UnitID);

				lbCode.Text = unit.Code;
				imgUnit.ImageUrl = string.Format("unitpics/{0}.png", unit.Code);
				lbName.Text = unit.Name;
				lbDescription.Text = unit.Description;
				tbNote.Text = unit.CurrentStatus;
				ddLicense.SelectedValue = unit.LicenseType.ToString();
				tbModelProg.Text = unit.ModelProgress.ToString();
				tbTextureProg.Text = unit.TextureProgress.ToString();
				tbScriptProg.Text = unit.ScriptProgress.ToString();
				lbLastChanged.Text = unit.User != null ? unit.User.Login : "";
				lbGame.Text = unit.Game.Name;
                

				FillCandidates(unit);

				if (Global.LoggedUser == null || !Global.LoggedUser.IsAdmin) {
					btnSubmit.Enabled = false;
					btnRemove.Enabled = false;
					tbNote.ReadOnly = true;
					ddLicense.Enabled = false;
					tbModelProg.ReadOnly = true;
					tbTextureProg.ReadOnly = true;
					tbScriptProg.ReadOnly = true;

				}
			}

		}

		private void FillCandidates(Unit unit)
		{
			var sb = new StringBuilder();
			sb.Append("<table><tr>");
			ddCandidates.Items.Clear();
			foreach (var x in unit.Candidates)
			{
				ddCandidates.Items.Add(new ListItem(x.Model.User.Login + "'s " +x.Model.Name, x.ModelID.ToString()));
				sb.AppendFormat("<td><a href='ModelDetail.aspx?ModelID={0}'><img src='{3}' class='thumbIcon'/><br/>{1}'s {2}</a></td>", x.ModelID, x.Model.User.Login, x.Model.Name, x.Model.GetIconUrl());
			}
			sb.Append("</tr></table>");
			litCandidates.Text = sb.ToString();


		}

		protected void btnSubmit_Click(object sender, EventArgs e)
		{
			var unit = Global.Db.Units.Single(x => x.UnitID == UnitID);
			unit.LicenseType = int.Parse(ddLicense.SelectedValue);
			unit.ModelProgress = int.Parse(tbModelProg.Text);
			unit.TextureProgress = int.Parse(tbTextureProg.Text);
			unit.ScriptProgress = int.Parse(tbScriptProg.Text);
			unit.CurrentStatus = tbNote.Text;
			Global.AddEvent(EventType.UnitUpdated, null, unit.UnitID, null, string.Format("License: {0}\n{1}\n", unit.LicenseType, unit.CurrentStatus));
			Global.Db.SubmitChanges();
		}

		protected void btnRemove_Click(object sender, EventArgs e)
		{
			var unit = Global.Db.Units.Single(x => x.UnitID == UnitID);
			var cand = unit.Candidates.SingleOrDefault(x => x.ModelID == int.Parse(ddCandidates.SelectedValue));
			if (cand != null)
			{
				Global.Db.Candidates.DeleteOnSubmit(cand);
				Global.Db.SubmitChanges();
				FillCandidates(unit);
			}

		}
	}
}
