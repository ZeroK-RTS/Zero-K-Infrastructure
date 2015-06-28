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

		public LoginForm(bool register = false)
		{
            InitializeComponent();
            if ((string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) && string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) || register) tabControl1.SelectedTab = tabPage2; // register as primary no data about pass and name
            tbLogin.Text = Program.Conf.LobbyPlayerName;
		    Program.SteamHandler.SteamHelper.SteamOnline += SteamApiOnSteamOnline;
            rgName.Text = Program.SteamHandler.SteamName;
			tbPassword.Text = Program.Conf.LobbyPlayerPassword;
		}

	    void SteamApiOnSteamOnline()
	    {
	        Program.MainWindow.InvokeFunc(() =>
	        {
	            rgName.Text = Program.SteamHandler.SteamName;
	            Program.SteamHandler.SteamHelper.SteamOnline -= SteamApiOnSteamOnline;
	        });
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
            Icon = ZklResources.ZkIcon;
        }
	}
}