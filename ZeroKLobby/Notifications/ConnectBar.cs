using System.Drawing;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby.Notifications
{
    /// <summary>
    ///     Handles connection to tasclient
    /// </summary>
    internal class ConnectBar: ZklNotifyBar
    {
        private static bool tasClientConnectCalled;
        private readonly bool canRegister = false;
        private readonly TasClient client;
        private readonly object tryConnectLocker = new object();
        private BitmapButton btnDetail;
        private Label lbState;


        public ConnectBar(TasClient tasClient): this() {
            client = tasClient;

            client.ConnectionLost += (s, e) =>
            {
                {
                    if (!client.WasDisconnectRequested) lbState.Text = "disconnected, reconnecting...";
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
                lbState.Text = "Connected, logging in ...";
                if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) LoginWithDialog("Please choose your name and password.\nThis will create a new account if it does not exist.");
                else client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
            };

            client.LoginAccepted += (s, e) => { Program.NotifySection.RemoveBar(this); };

            client.LoginDenied += (s, e) =>
            {
                if (e.ResultCode == LoginResponse.Code.InvalidName && !string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword))
                {
                    if (
                        MessageBox.Show(NavigationControl.Instance, //new Form { TopMost = true },
                            string.Format("Account '{0}' does not exist yet, do you want to create it?", Program.Conf.LobbyPlayerName),
                            "Confirm account registration",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        lbState.Text = "Registering a new account";
                        client.Register(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
                    } else LoginWithDialog(string.Format("Login denied: {0} {1}", e.ResultCode.Description(), e.Reason));
                } else
                {
                    LoginWithDialog(
                        string.Format("Login denied: {0} {1}\nChoose a different name to create new account.", e.ResultCode.Description(), e.Reason));
                }
            };

            client.RegistrationDenied +=
                (s, e) => LoginWithDialog(string.Format("Registration denied: {0} {1}", e.ResultCode.Description(), e.Reason));

            client.RegistrationAccepted += (s, e) => client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
        }


        public ConnectBar() {
            InitializeComponent();
        }


        public void TryToConnectTasClient() {
            lock (tryConnectLocker)
            {
                if (!tasClientConnectCalled && !client.IsConnected && !client.IsLoggedIn)
                {
                    //Program.NotifySection.AddBar(this);
                    tasClientConnectCalled = true;
                    lbState.Text = "Trying to connect ...";
                    client.Connect(Program.Conf.SpringServerHost, Program.Conf.SpringServerPort);
                }
            }
        }

        private void InitializeComponent() {
            SuspendLayout();

            btnDetail = new BitmapButton { Text = "Connect", Left = 10, Top = 10, Width = 100, Height = 60, Font = Config.GeneralFontBig };
            btnDetail.Click += (sender, args) => TryToConnectTasClient();
            Controls.Add(btnDetail);

            lbState = new Label
            {
                AutoSize = true,
                Location = new Point(120, 30),
                Name = "lbState",
                ForeColor = Config.TextColor,
                BackColor = Color.Transparent,
                Text = "Connect to the Spring multiplayer lobby server."
            };

            Controls.Add(lbState);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Size = new Size(300, 80);

            ResumeLayout(false);
        }

        private void LoginWithDialog(string text) {
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
                if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) MessageBox.Show(new Form { TopMost = true }, "Please fill player name and password", "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } while (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword));
            Program.SaveConfig();
            if (canRegister) client.Register(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
            else client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
        }
    }
}