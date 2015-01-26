using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Notifications
{
	/// <summary>
	/// Handles connection to tasclient
	/// </summary>
	class ConnectBar: UserControl, INotifyBar
	{
		bool canRegister = false;
		readonly TasClient client;

		Label lbState;
		static bool tasClientConnectCalled;
		readonly object tryConnectLocker = new object();

		public ConnectBar(TasClient tasClient): this()
		{
			client = tasClient;

			
			client.ConnectionLost += (s, e) =>
				{
					canRegister = false;
					{
						if (client.ConnectionFailed) lbState.Text = "disconnected due to network problem, autoreconnecting...";
						else
						{
							lbState.Text = "disconnected";
							tasClientConnectCalled = false;
						}
						Program.NotifySection.AddBar(this);
					}
				};

			client.Connected += (s, e) =>
				{
					canRegister = false;
					Program.NotifySection.RemoveBar(this);
					lbState.Text = "Connected, logging in ...";
                    if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) LoginWithDialog("Please enter your name and password", true);
					else client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
				};

			client.LoginAccepted += (s, e) => Program.NotifySection.RemoveBar(this);

			client.LoginDenied += (s, e) =>
				{
                    if (e.ServerParams[0] == "Bad username/password" && !string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword) && canRegister)
					{
						lbState.Text = "Registering new account";
						client.Register(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
					}
					else LoginWithDialog("Login denied: " + e.ServerParams[0], false);
				};

			client.RegistrationDenied += (s, e) => LoginWithDialog("Registration denied: " + e.ServerParams[0], true);

			client.RegistrationAccepted += (s, e) => client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);

			client.AgreementRecieved += (s, e) =>
				{
					lbState.Text = "Waiting to accept agreement";
					var acceptForm = new AcceptAgreementForm { AgreementText = e.Text };
					if (acceptForm.ShowDialog() == DialogResult.OK)
					{
						lbState.Text = "Sending accept agreement";
						client.AcceptAgreement();
						ZkData.Utils.SafeThread(() =>
						{
							if (!Program.CloseOnNext) client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
						}).Start();
					}
					else
					{
						lbState.Text = "did not accept agreement";
						ZkData.Utils.SafeThread(() =>
						{
							if (!Program.CloseOnNext) client.RequestDisconnect(); //server will re-ask AcceptAgreement if we re-connect
						}).Start();

					}
				};


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