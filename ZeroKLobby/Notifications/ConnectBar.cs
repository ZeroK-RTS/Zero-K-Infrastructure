using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby.Notifications
{
	/// <summary>
	/// Handles connection to tasclient
	/// </summary>
	public class ConnectBar: UserControl
	{
		bool canRegister = false;
	    TasClient client;

		Label lbState;
		static bool tasClientConnectCalled;
        private PictureBox pictureBox1;
        private BitmapButton btnProfile;
        private BitmapButton btnLogout;
		readonly object tryConnectLocker = new object();

	    public void Init(TasClient tasClient)
	    {
	        client = tasClient;

	        client.ConnectionLost += (s, e) => {
	            canRegister = false;
	            {
	                if (!client.WasDisconnectRequested) lbState.Text = "disconnected due to network problem, autoreconnecting...";
	                else {
	                    lbState.Text = "disconnected";
	                    tasClientConnectCalled = false;
	                }
	            }
	        };

	        client.Connected += (s, e) => {
	            canRegister = false;
	            lbState.Text = "Connected, logging in ...";
	            if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) LoginWithDialog("Please enter your name and password", true);
	            else client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
	        };

	        client.LoginAccepted += (s, e) => { lbState.Text = client.UserName; };

	        client.LoginDenied += (s, e) => {
	            if (e.ResultCode == LoginResponse.Code.InvalidName && !string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword) && canRegister) {
	                lbState.Text = "Registering new account";
	                client.Register(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
	            } else LoginWithDialog(string.Format("Login denied: {0} {1}", e.ResultCode, e.Reason), false);
	        };

	        client.RegistrationDenied += (s, e) => LoginWithDialog(string.Format("Registration denied: {0} {1}", e.ResultCode.Description(), e.Reason), true);

	        client.RegistrationAccepted += (s, e) => client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
	    }


	    public ConnectBar()
		{
			InitializeComponent();
	        this.Font = Config.GeneralFont;
	        //this.ForeColor = Program.Conf.TextColor;
		}

		public void TryToConnectTasClient()
		{
			lock (tryConnectLocker)
			{
				if (!tasClientConnectCalled && !client.IsConnected && !client.IsLoggedIn)
				{
					tasClientConnectCalled = true;
					lbState.Text = "Trying to connect ...";
					client.Connect(Program.Conf.SpringServerHost, Program.Conf.SpringServerPort);
				}
			}
		}

		void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectBar));
            this.lbState = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnLogout = new ZeroKLobby.BitmapButton();
            this.btnProfile = new ZeroKLobby.BitmapButton();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // lbState
            // 
            this.lbState.AutoSize = true;
            this.lbState.Location = new System.Drawing.Point(70, 11);
            this.lbState.Name = "lbState";
            this.lbState.Size = new System.Drawing.Size(30, 13);
            this.lbState.TabIndex = 0;
            this.lbState.Text = "state";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(64, 64);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // btnLogout
            // 
            this.btnLogout.BackColor = System.Drawing.Color.Transparent;
            this.btnLogout.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnLogout.BackgroundImage")));
            this.btnLogout.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnLogout.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnLogout.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogout.FlatAppearance.BorderSize = 0;
            this.btnLogout.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnLogout.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Location = new System.Drawing.Point(154, 38);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(75, 23);
            this.btnLogout.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnLogout.TabIndex = 3;
            this.btnLogout.Text = "Logout";
            this.btnLogout.UseVisualStyleBackColor = false;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // btnProfile
            // 
            this.btnProfile.BackColor = System.Drawing.Color.Transparent;
            this.btnProfile.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnProfile.BackgroundImage")));
            this.btnProfile.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnProfile.ButtonStyle = ZeroKLobby.FrameBorderRenderer.StyleType.DarkHive;
            this.btnProfile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnProfile.FlatAppearance.BorderSize = 0;
            this.btnProfile.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.btnProfile.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.btnProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnProfile.ForeColor = System.Drawing.Color.White;
            this.btnProfile.Location = new System.Drawing.Point(73, 38);
            this.btnProfile.Name = "btnProfile";
            this.btnProfile.Size = new System.Drawing.Size(75, 23);
            this.btnProfile.SoundType = ZeroKLobby.Controls.SoundPalette.SoundType.Click;
            this.btnProfile.TabIndex = 2;
            this.btnProfile.Text = "Profile";
            this.btnProfile.UseVisualStyleBackColor = false;
            this.btnProfile.Click += new System.EventHandler(this.btnProfile_Click);
            // 
            // ConnectBar
            // 
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.btnLogout);
            this.Controls.Add(this.btnProfile);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lbState);
            this.MinimumSize = new System.Drawing.Size(300, 60);
            this.Name = "ConnectBar";
            this.Size = new System.Drawing.Size(300, 64);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

	    void LoginWithDialog(string text, bool register = false)
		{
			do
			{
                var loginForm = new LoginForm(register);
				loginForm.InfoText = text;
				if (loginForm.ShowDialog() == DialogResult.Cancel) 
				{
					tasClientConnectCalled = false;
					client.RequestDisconnect();
					lbState.Text = "Login cancelled, press button on left to login again";
					return;
				}
				canRegister = loginForm.CanRegister;
				Program.Conf.LobbyPlayerName = loginForm.LoginValue;
				Program.Conf.LobbyPlayerPassword = loginForm.PasswordValue;
				if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) MessageBox.Show("Please fill player name and password", "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Information); 
			} while (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword));
			Program.SaveConfig();
			if (canRegister)
			{
			  client.Register(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
			}
			else
			{
			  client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
			}
		}

        private void btnLogout_Click(object sender, System.EventArgs e)
        {
            Program.TasClient.RequestDisconnect();
            Program.Conf.LobbyPlayerName = "";
            Program.MainWindow.connectBar.TryToConnectTasClient();
        }

        private void btnProfile_Click(object sender, System.EventArgs e)
        {
            Program.BrowserInterop.OpenUrl(string.Format("{0}/", GlobalConst.BaseSiteUrl));
        }


	}
}