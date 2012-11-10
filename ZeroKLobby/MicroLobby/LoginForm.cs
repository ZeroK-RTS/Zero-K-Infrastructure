using System;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
	public partial class LoginForm: Form
	{
		public string InfoText { set { lbInfo.Text = value; } }

		public string LoginValue {
            get
            {
                if (!CanRegister) return tbLogin.Text;
                else return rgName.Text;
            } 
        }

		public string PasswordValue { 
            get {
                if (!CanRegister) return tbPassword.Text;
                else return rgPassword.Text;;
            }
        }

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
			Icon = Resources.ZkIcon;
        }
	}
}