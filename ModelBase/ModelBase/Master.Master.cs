#region using

using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySql.Data.MySqlClient;

#endregion

namespace ModelBase
{
	public partial class Master : MasterPage
	{
		#region Other methods

		protected void btnLogOut_Click(object sender, EventArgs e)
		{
			Global.LoggedUserID = null;
			lbLogin.Text = "Logged out";
			btnLogOut.Visible = false;
			Login1.Visible = true;
			tbControl.Visible = true;
			lbControl.Visible = true;
			Response.Cookies.Add(new HttpCookie("pw", null));
			Response.Cookies.Add(new HttpCookie("login", null));
		}

		protected void Login1_Authenticate(object sender, AuthenticateEventArgs e)
		{
			string pasHash = Hash.HashString(Login1.Password).ToString();
			User user = Global.Db.Users.SingleOrDefault(x => x.Login == Login1.UserName);
			if (user == null) {
				user = new User();
				foreach (var chr in Login1.UserName) {
					if ((Char.IsLetterOrDigit(chr) && chr!='[' && chr!=']' && !Path.GetInvalidFileNameChars().Contains(chr))  ||  chr == '_') continue; else throw new ApplicationException("Password contains invalid character " + chr);
				}
				if (tbControl.Text != "upspring" || string.IsNullOrEmpty(Login1.UserName) || string.IsNullOrEmpty(Login1.Password)) {
					Login1.FailureText = "Control question answerred incorrectly or missing username/password";
					e.Authenticated = false;
					return;
				}

				user.Login = Login1.UserName;
				user.Password = pasHash;
				user.PasswordText = Login1.Password;
				Global.Db.Users.InsertOnSubmit(user);

				new SvnController().UpdateAddFolderAndCommit(Login1.UserName);
				Global.Db.SubmitChanges();
				e.Authenticated = true;
				new SvnConfigMaker().Generate();
			} else {
				if (user.Password == pasHash && !user.IsDeleted) {
					e.Authenticated = true;
					if (string.IsNullOrEmpty(user.PasswordText)) { // fill up missing password text
						user.PasswordText = Login1.Password;
						Global.Db.SubmitChanges();
						new SvnConfigMaker().Generate();
                    }
				}
				
				else {
					Login1.FailureText = "Login incorrect";
					e.Authenticated = false;
				}
			}


			if (e.Authenticated) {
				Global.LoggedUserID = user.UserID;
				lbLogin.Text = "Logged as " + user.Login;
				btnLogOut.Visible = true;
				Login1.Visible = false;
				tbControl.Visible = false;
				lbControl.Visible = false;
				if (Login1.RememberMeSet) {
					Response.Cookies.Add(new HttpCookie("pw", pasHash) {Expires = DateTime.Now.AddYears(5)});
					Response.Cookies.Add(new HttpCookie("login", user.Login){Expires = DateTime.Now.AddYears(5)});
				} else {
					Response.Cookies.Add(new HttpCookie("pw", null));
					Response.Cookies.Add(new HttpCookie("login", null));
				}
			} else {
				Global.LoggedUserID = null;
				btnLogOut.Visible = false;
				tbControl.Visible = true;
				lbControl.Visible = true;
				lbLogin.Text = "Login failed - incorrect password";
			}
		}

		protected void Page_Load(object sender, EventArgs e)
		{
		}

		protected void Page_Init(object sender, EventArgs e)
		{
			if (!IsPostBack && Global.LoggedUserID == null)
			{
				string pw = Request["pw"];
				string login = Request["login"];
				if (login != null && pw != null)
				{
					User user = Global.Db.Users.SingleOrDefault(x => x.Login == login && x.Password == pw);
					if (user != null)
					{
						Global.LoggedUserID = user.UserID;
						lbLogin.Text = "Logged as " + user.Login;
					}
				}
			}

			if (Global.LoggedUserID == null)
			{
				btnLogOut.Visible = false;
				Login1.Visible = true;
				tbControl.Visible = true;
				lbControl.Visible = true;
			}
			else
			{
				tbControl.Visible = false;
				lbControl.Visible = false;
				btnLogOut.Visible = true;
				Login1.Visible = false;
				lbLogin.Text = Global.LoggedUser.Login;
			}
			
		}

		#endregion
	}
}