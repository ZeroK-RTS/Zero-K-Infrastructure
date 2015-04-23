using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
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
	    TasClient client;

		Label lbState;
		static bool tasClientConnectCalled;
        private PictureBox pictureBox1;
        private BitmapButton btnProfile;
        private BitmapButton btnLogout;
		readonly object tryConnectLocker = new object();
	    PlayerListItem playerItem;

	    public void Init(TasClient tasClient)
	    {
	        client = tasClient;

	        client.ConnectionLost += (s, e) => {
	            {
	                if (!client.WasDisconnectRequested) lbState.Text = "disconnected, reconnecting...";
	                else {
	                    lbState.Text = "disconnected";
	                    tasClientConnectCalled = false;
	                }
	            }
	        };

	        client.Connected += (s, e) => {
                btnLogout.Text = "Logout";
	            lbState.Text = "Connected, logging in ...";
	            if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) LoginWithDialog("Please choose your name and password.\nThis will create a new account if it does not exist.");
	            else client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
	        };

	        client.LoginAccepted += (s, e) => {
	            lbState.Text = client.UserName;
	            pictureBox1.Image = Program.ServerImages.GetAvatarImage(client.MyUser);
	            playerItem = new PlayerListItem() { UserName = client.UserName };
	        };

	        client.LoginDenied += (s, e) => {
	            if (e.ResultCode == LoginResponse.Code.InvalidName && !string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) {
	                if (
	                    MessageBox.Show(string.Format("Account '{0}' does not exist yet, do you want to create it?", Program.Conf.LobbyPlayerName), "Confirm account registration",
	                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
	                    lbState.Text = "Registering a new account";
	                    client.Register(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
	                } else {
                        LoginWithDialog(string.Format("Login denied: {0} {1}", e.ResultCode.Description(), e.Reason));
	                }
	            } else {
	                LoginWithDialog(string.Format("Login denied: {0} {1}\nChoose a different name to create new account.", e.ResultCode.Description(), e.Reason));
	            }
	        };

	        client.RegistrationDenied += (s, e) => LoginWithDialog(string.Format("Registration denied: {0} {1}", e.ResultCode.Description(), e.Reason));

	        client.RegistrationAccepted += (s, e) => client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
	    }


	    public ConnectBar()
		{
			InitializeComponent();
	        this.Font = Config.GeneralFont;
	        btnProfile.Image = Buttons.link.GetResized(16, 16);
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
            this.btnLogout.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
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
            this.btnProfile.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
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

	    void LoginWithDialog(string text)
		{
			do
			{
                var loginForm = new LoginForm();
				loginForm.InfoText = text;
			    //loginForm.Parent = this;
                if (loginForm.ShowDialog() == DialogResult.Cancel) 
				{
			        DoLogout();
					return;
				}
				Program.Conf.LobbyPlayerName = loginForm.LoginValue;
				Program.Conf.LobbyPlayerPassword = loginForm.PasswordValue;
				if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) MessageBox.Show("Please fill player name and password", "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Information); 
			} while (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword));
			Program.SaveConfig();
    	    client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
		}

        private void btnLogout_Click(object sender, System.EventArgs e)
        {
            if (client.IsLoggedIn) DoLogout();
            else TryToConnectTasClient();
        }

	    public void DoLogout()
	    {
            tasClientConnectCalled = false;
            client.RequestDisconnect();
	        Program.Conf.LobbyPlayerName = "";
            Program.Conf.LobbyPlayerPassword = "";
            lbState.Text = "Press button to login again";
            btnLogout.Text = "Login";
	        pictureBox1.Image = null;
	        playerItem = null;
	    }

        private void btnProfile_Click(object sender, System.EventArgs e)
        {
            Program.BrowserInterop.OpenUrl(string.Format("{0}/", GlobalConst.BaseSiteUrl));
        }


	    protected override void OnPaint(PaintEventArgs e)
	    {
	        if (playerItem != null) {
	            lbState.Visible = false;
	        } else lbState.Visible = true;
            base.OnPaint(e);
	        if (playerItem != null) {
	            playerItem.DrawPlayerLine(e.Graphics, lbState.Bounds, Program.Conf.TextColor, false,false);
	        }
            
            
	    }
	}
}