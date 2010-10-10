#region using

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

#endregion

namespace ModelBase
{
	public partial class ModelDetailForm : Page
	{
		#region Fields

		protected int ModelID
		{
			get { return int.Parse(Request["ModelID"]); }
		}

		#endregion

		#region Other methods

		private void AddScreenshots(Model model)
		{
			string dirpath = Server.MapPath(string.Format("~/svn/{0}/{1}", model.User.Login, model.Name));
			if (Directory.Exists(dirpath)) {
				int cnt = 0;
				int len = 5;
				StringBuilder sb = new StringBuilder();


				sb.AppendLine("<table>");
				
				/*string icon = dirpath + "/icon.png";
				if (File.Exists(icon) && new FileInfo(icon).Length > 20000) {
					sb.AppendLine("<tr>");
					string url = string.Format("svn/{0}/{1}/icon.png", model.User.Login, model.Name);
					sb.AppendFormat("<td><a href='{0}'><img src='{0}' class='thumbScreenshot'/></a></td>\n", url);
					cnt++;
				}*/

				
				foreach (string s in Directory.GetFiles(dirpath, "*.jpg")) {
					if (cnt%len == 0) sb.AppendLine("<tr>");

					string url = string.Format("svn/{0}/{1}/{2}", model.User.Login, model.Name, Path.GetFileName(s));

					sb.AppendFormat("<td><a href='{0}'><img src='{0}' class='thumbScreenshot'/></a></td>\n", url);

					if (cnt%len == len - 1) sb.AppendLine("</tr>");
					cnt++;
				}

				if (Directory.Exists(dirpath + "/screenshots")) {
					foreach (string s in Directory.GetFiles(dirpath + "/screenshots")) {
						if (cnt%len == 0) sb.AppendLine("<tr>");

						string url = string.Format("svn/{0}/{1}/screenshots/{2}", model.User.Login, model.Name, Path.GetFileName(s));

						sb.AppendFormat("<td><a href='{0}'><img src='{0}' class='thumbScreenshot'/></a></td>\n", url);

						if (cnt%len == len - 1) sb.AppendLine("</tr>");
						cnt++;
					}
				}
				if (cnt % len != len) sb.AppendLine("</tr>");
				sb.AppendLine("</table>");
				Literal1.Text = sb.ToString();
			}
		}

		protected void btnAdd_Click(object sender, EventArgs e)
		{
			Unit unit = Global.Db.Units.SingleOrDefault(x => x.Code == tbCandCode.Text && x.GameID == int.Parse(ddGames.SelectedValue));
			if (unit != null) {
				Model model = Global.Db.Models.Single(x => x.ModelID == ModelID);
				if (!model.Candidates.Any(x => x.UnitID == unit.UnitID)) {
					model.Candidates.Add(new Candidate {UnitID = unit.UnitID});
					Global.Db.SubmitChanges();
					tbCandCode.Text = "";
					FillCandidates(model);
				}
			}
		}

		protected void btnRemove_Click(object sender, EventArgs e)
		{
			Model model = Global.Db.Models.Single(x => x.ModelID == ModelID);
			Candidate cand = model.Candidates.SingleOrDefault(x => x.UnitID == int.Parse(ddCandidates.SelectedValue));
			if (cand != null) {
				Global.Db.Candidates.DeleteOnSubmit(cand);
				Global.Db.SubmitChanges();
				FillCandidates(model);
			}
		}

		protected void btnSubmit_Click(object sender, EventArgs e)
		{
			Model model = Global.Db.Models.Single(x => x.ModelID == ModelID);
			if (Global.LoggedUser != null && (Global.LoggedUser.IsAdmin || Global.LoggedUserID == model.AuthorUserID)) {
				model.Description = tbDescription.Text;
				model.ModelProgress = int.Parse(tbModelProg.Text);
				model.TextureProgress = int.Parse(tbTextureProg.Text);
				model.ScriptProgress = int.Parse(tbScriptProg.Text);
				model.LicenseID = int.Parse(ddLicense.SelectedValue);
				model.ModelTags.Clear();
				foreach (ListItem item in ddTags.Items) if (item.Selected) model.ModelTags.Add(new ModelTag {TagID = int.Parse(item.Value)});
				Global.Db.SubmitChanges();
				lbTags.Text = Global.PrintTags(model.ModelTags);
			}
		}

		private void FillCandidates(Model model)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<table><tr>");
			ddCandidates.Items.Clear();
			foreach (Candidate x in model.Candidates) {
				ddCandidates.Items.Add(new ListItem(x.Unit.Game.Shortcut + " "  + x.Unit.Code, x.UnitID.ToString()));
				sb.AppendFormat("<td><a href='UnitDetail.aspx?UnitID={0}'><img src='unitpics/{1}.png'/><br/>{2} {1}</a></td>", x.UnitID, x.Unit.Code, x.Unit.Game.Shortcut);
			}
			sb.Append("</tr></table>");
			litCandidates.Text = sb.ToString();
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			Model model = Global.Db.Models.Single(x => x.ModelID == ModelID);
			if (!IsPostBack) {
				if (Request["ModelID"] != null) UcComments1.ModelID = ModelID;
				UcComments1.DataBind();

				linkSources.Text = "Download model sources";
				linkSources.NavigateUrl = string.Format("http://springrts.com/websvn/listing.php?repname=ModelBase&path=%2F{0}%2F{1}%2F#path_{0}_{1}_",
				                                        model.User.Login,
				                                        model.Name);


				lbName.Text = model.Name;
				imgUnit.ImageUrl = model.GetIconUrl();
				tbDescription.Text = model.Description;
				lbTags.Text = Global.PrintTags(model.ModelTags);
				lbModified.Text = model.Modified.ToString();
				lbAuthor.Text = model.User.Login;
				tbModelProg.Text = model.ModelProgress.ToString();
				tbTextureProg.Text = model.TextureProgress.ToString();
				tbScriptProg.Text = model.ScriptProgress.ToString();

				ddGames.Items.Clear();
				foreach (var g in Global.Db.Games.OrderBy(x=>x.Shortcut)) {
					ddGames.Items.Add(new ListItem(g.Shortcut, g.GameID.ToString()));
				}


				ddLicense.Items.Clear();
				foreach (var l in Global.Db.Licenses.OrderBy(x=>x.Name)) {
					ddLicense.Items.Add(new ListItem(l.Name, l.LicenseID.ToString()) {Selected = l.LicenseID == model.LicenseID});
				}
				UpdateLicenseLink(model);

				foreach (Tag tag in Global.Db.Tags.OrderBy(x => x.Name)) {
					ListItem li = new ListItem(tag.Name, tag.TagID.ToString()) {Selected = model.ModelTags.Select(x => x.TagID).Contains(tag.TagID)};
					ddTags.Items.Add(li);
				}


				FillCandidates(model);

				if (Global.LoggedUser == null || (!Global.LoggedUser.IsAdmin && Global.LoggedUserID != model.AuthorUserID)) {
					tbDescription.ReadOnly = true;
					btnSubmit.Enabled = false;
					ddTags.Visible = false;
					btnRemove.Enabled = false;
					tbModelProg.ReadOnly = true;
					tbScriptProg.ReadOnly = true;
					tbTextureProg.ReadOnly = true;
					ddLicense.Enabled = false;
				}
			}


			AddScreenshots(model);
		}

		private void UpdateLicenseLink(Model model) {
			var lic = Global.Db.Licenses.SingleOrDefault(x => x.LicenseID == int.Parse(ddLicense.SelectedValue));
			if (lic != null) {
				hlLicense.Text = lic.Name;
				string licFile = string.Format("svn/{0}/{1}/license.txt", model.User.Login, model.Name);
				if (File.Exists(Server.MapPath(licFile))) {
					hlLicense.NavigateUrl = licFile;
				} else hlLicense.NavigateUrl = lic.DefaultUrl;
			}
		}

		#endregion

		protected void ddLicense_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateLicenseLink(Global.Db.Models.Single(m => m.ModelID == ModelID));
		}
	}
}