using System;
using System.Windows.Forms;
using PlanetWars.Properties;
using PlanetWarsShared;


namespace PlanetWars.UI
{
	public partial class LoginForm : Form
	{
		public LoginForm()
		{
			InitializeComponent();
			userBox.Text = Settings.Default.UserName;
			if (Settings.Default.RememberPassword) {
				passwordBox.Text = Settings.Default.Password;
			} else {
				passwordBox.Text = "guest";
			}
			checkBox1.Checked = Settings.Default.RememberPassword;
			serverBox.Text = Settings.Default.ServerUrl;
			button1.Click += button1_Click;
			
		}

		public AuthInfo AuthInfo
		{
			get { return new AuthInfo {Login = userBox.Text, Password = passwordBox.Text}; }
		}

		void button1_Click(object sender, EventArgs e)
		{
			SubmitForm();
		}

		void FormKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 13) {
				SubmitForm();
			}
		}

		void SubmitForm()
		{
			Settings.Default.UserName = userBox.Text;
			if (checkBox1.Checked) {
				Settings.Default.RememberPassword = true;
				Settings.Default.Password = passwordBox.Text;
			} else {
				Settings.Default.RememberPassword = false;
				Settings.Default.Password = String.Empty;
			}
			Settings.Default.ServerUrl = serverBox.Text;
  		string serverString = string.Format("tcp://{0}/IServer",serverBox.Text);

			try
			{
				Program.Server = (IServer)Activator.GetObject(typeof(IServer), serverString);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					string.Format("Error setting up server:{0}", ex),
					string.Format("Connecting to {0}", serverString),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}


			Settings.Default.Save();
			DialogResult = DialogResult.OK;
		}
	}
}