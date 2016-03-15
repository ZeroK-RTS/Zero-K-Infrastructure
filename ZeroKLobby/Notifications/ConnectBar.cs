using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby.Controls;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby.Notifications
{
    /// <summary>
    ///     Handles connection to tasclient
    /// </summary>
    internal class ConnectBar: ZklBaseControl, INotifyBar
    {
        private static bool tasClientConnectCalled;
        private readonly TasClient client;
        private readonly object tryConnectLocker = new object();
        private readonly bool canRegister = false;

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

            client.LoginAccepted += (s, e) =>
            {
                Program.NotifySection.RemoveBar(this);
                //Program.MainWindow.navigationControl.Path = "battles";
            };

            client.LoginDenied += (s, e) =>
            {
                if (e.ResultCode == LoginResponse.Code.InvalidName && !string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword))
                {
                    if (
                        MessageBox.Show(
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

        public void AddedToContainer(NotifyBarContainer container) {
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

        public void DetailClicked(NotifyBarContainer container) {
            TryToConnectTasClient();
        }

        public Control GetControl() {
            return this;
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
            lbState = new Label();
            SuspendLayout();
            // 
            // lbState
            // 
            lbState.AutoSize = true;
            lbState.Location = new Point(14, 19);
            lbState.Name = "lbState";
            lbState.Size = new Size(225, 13);
            lbState.TabIndex = 0;
            lbState.Text = "Connect to the Spring multiplayer lobby server.";
            // 
            // ConnectBar
            // 
            BackColor = Color.Transparent;
            Controls.Add(lbState);
            MinimumSize = new Size(300, 60);
            Name = "ConnectBar";
            Size = new Size(364, 60);
            ResumeLayout(false);
            PerformLayout();
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
                if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) MessageBox.Show("Please fill player name and password", "Missing information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } while (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword));
            Program.SaveConfig();
            if (canRegister) client.Register(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
            else client.Login(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
        }
    }
}