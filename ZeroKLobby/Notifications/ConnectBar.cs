using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby.Controls;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby.Notifications
{
	/// <summary>
	/// Handles connection to tasclient
	/// </summary>
	class ConnectBar: ZklBaseControl, INotifyBar
	{
		bool canRegister = false;
		readonly TasClient client;

		Label lbState;
		static bool tasClientConnectCalled;
		readonly object tryConnectLocker = new object();

		public ConnectBar(TasClient tasClient): this()
		{
			client = tasClient;


            client.ConnectionLost += (s, e) => {
                {
                        if (!client.WasDisconnectRequested) lbState.Text ="disconnected, reconnecting...";
                        else
                        {
                            lbState.Text = "disconnected";
                            tasClientConnectCalled = false;
                        }
                        Program.NotifySection.AddBar(this);
                }
            };

            client.Connected += (s, e) => {
                lbState.Text = "Connected, logging in ...";
                if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) LoginWithDialog("Please choose your name and password.\nThis will create a new account if it does not exist.");
                else client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
            };

            client.LoginAccepted += (s, e) => Program.NotifySection.RemoveBar(this);

            client.LoginDenied += (s, e) => {
                if (e.ResultCode == LoginResponse.Code.InvalidName && !string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword))
                {
                    if (
                        MessageBox.Show(string.Format("Account '{0}' does not exist yet, do you want to create it?", Program.Conf.LobbyPlayerName), "Confirm account registration",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        lbState.Text = "Registering a new account";
                        client.Register(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
                    }
                    else
                    {
                        LoginWithDialog(string.Format("Login denied: {0} {1}", e.ResultCode.Description(), e.Reason));
                    }
                }
                else
                {
                    LoginWithDialog(string.Format("Login denied: {0} {1}\nChoose a different name to create new account.", e.ResultCode.Description(), e.Reason));
                }
            };

            client.RegistrationDenied += (s, e) => LoginWithDialog(string.Format("Registration denied: {0} {1}", e.ResultCode.Description(), e.Reason));

            client.RegistrationAccepted += (s, e) => client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
		}


		public ConnectBar()
		{
			InitializeComponent();
		}

		public void TryToConnectTasClient()
		{
			lock (tryConnectLocker)
			{
				if (!tasClientConnectCalled && !client.IsConnected && !client.IsLoggedIn)
				{
					Program.NotifySection.AddBar(this);
					tasClientConnectCalled = true;
					lbState.Text = "Trying to connect ...";
					client.Connect(Program.Conf.SpringServerHost, Program.Conf.SpringServerPort);
				}
			}
		}


	    void InitializeComponent()
		{
            this.lbState = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbState
            // 
            this.lbState.AutoSize = true;
            this.lbState.Location = new System.Drawing.Point(14, 19);
            this.lbState.Name = "lbState";
            this.lbState.Size = new System.Drawing.Size(225, 13);
            this.lbState.TabIndex = 0;
            this.lbState.Text = "Connect to the Spring multiplayer lobby server.";
            // 
            // ConnectBar
            // 
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.lbState);
            this.MinimumSize = new System.Drawing.Size(300, 60);
            this.Name = "ConnectBar";
            this.Size = new System.Drawing.Size(364, 60);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

	    void LoginWithDialog(string text, bool register = false)
		{
			do
			{
                var loginForm = new LoginForm();
				loginForm.InfoText = text;
				if (loginForm.ShowDialog(Program.MainWindow) == DialogResult.Cancel) 
				{
					tasClientConnectCalled = false;
					client.RequestDisconnect();
					lbState.Text = "Login cancelled, press button on left to login again";
					return;
				}
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

		public void AddedToContainer(NotifyBarContainer container)
		{
			container.btnDetail.ImageAlign = ContentAlignment.TopCenter;
			container.btnDetail.Text = "Connect";

			const int newSize = 20;
			var image = new Bitmap(newSize, newSize);
			using (var g = Graphics.FromImage(image))
			{
				g.InterpolationMode = InterpolationMode.High;
                g.DrawImage(ZklResources.redlight, 0, 0, newSize, newSize);
			}
			container.btnDetail.Image = image;
			container.btnStop.Visible = false;
		    container.Title = "Connecting to server";
		    container.TitleTooltip = "Check website for server status";
		}


		public void CloseClicked(NotifyBarContainer container) {}

		public void DetailClicked(NotifyBarContainer container)
		{
			TryToConnectTasClient();
		}

		public Control GetControl()
		{
			return this;
		}
	}
}