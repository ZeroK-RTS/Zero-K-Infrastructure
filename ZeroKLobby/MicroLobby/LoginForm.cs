using System;
using System.Windows.Forms;

namespace SpringDownloader.MicroLobby
{
	public partial class LoginForm: Form
	{
		public string InfoText { set { lbInfo.Text = value; } }

		public string LoginValue { get { return tbLogin.Text; } }

		public string PasswordValue { get { return tbPassword.Text; } }

		public bool CanRegister { get; private set; }

		public LoginForm()
		{
			InitializeComponent();
			tbLogin.Text = Program.Conf.LobbyPlayerName;
			tbPassword.Text = Program.Conf.LobbyPlayerPassword;
		}

		void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		void btnRegister_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			CanRegister = true;
			Close();
		}

		void btnSubmit_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void LoginForm_Load(object sender, EventArgs e)
		{
			Icon = Resources.SpringDownloader;
		}
	}
}