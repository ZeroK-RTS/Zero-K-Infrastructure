using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroKLobby.Controls;

namespace ZeroKLobby.MicroLobby
{
    public class LoginForm: ZklBaseForm
    {
        private readonly BitmapButton btnCancel;
        private readonly BitmapButton btnSubmit;
        private readonly Label label1;
        private readonly Label label2;
        private readonly Label lbInfo;

        private readonly TextBox tbLogin;
        private readonly TextBox tbPassword;


        public LoginForm() {
            Font = Config.GeneralFontBig;

            //BackColor = Color.Transparent;
            var resources = new ComponentResourceManager(typeof(LoginForm));
            btnSubmit = new BitmapButton();
            tbLogin = new TextBox();
            tbPassword = new TextBox();
            lbInfo = new Label();
            btnCancel = new BitmapButton();
            label1 = new Label();
            label2 = new Label();
            SuspendLayout();
            // 
            // btnSubmit
            // 
            btnSubmit.ButtonStyle = FrameBorderRenderer.StyleType.DarkHive;
            btnSubmit.Cursor = Cursors.Hand;
            btnSubmit.DialogResult = DialogResult.OK;
            btnSubmit.FlatStyle = FlatStyle.Flat;
            btnSubmit.ForeColor = Color.White;
            btnSubmit.Location = new Point(70, 255);
            btnSubmit.Name = "btnSubmit";
            btnSubmit.Size = new Size(104, 44);
            btnSubmit.SoundType = SoundPalette.SoundType.Click;
            btnSubmit.TabIndex = 3;
            btnSubmit.Text = "OK";
            btnSubmit.UseVisualStyleBackColor = true;
            btnSubmit.Click += btnSubmit_Click;
            // 
            // tbLogin
            // 
            tbLogin.Location = new Point(237, 123);
            tbLogin.Name = "tbLogin";
            tbLogin.Size = new Size(146, 24);
            tbLogin.TabIndex = 1;
            // 
            // tbPassword
            // 
            tbPassword.Location = new Point(237, 184);
            tbPassword.Name = "tbPassword";
            tbPassword.Size = new Size(146, 24);
            tbPassword.TabIndex = 2;
            tbPassword.UseSystemPasswordChar = true;
            // 
            // lbInfo
            // 
            lbInfo.BackColor = Color.Transparent;
            lbInfo.ForeColor = Color.Red;
            lbInfo.Location = new Point(29, 20);
            lbInfo.Name = "lbInfo";
            lbInfo.Size = new Size(414, 51);
            lbInfo.TabIndex = 5;
            lbInfo.Text = "Error";
            lbInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.Transparent;
            btnCancel.BackgroundImageLayout = ImageLayout.Stretch;
            btnCancel.ButtonStyle = FrameBorderRenderer.StyleType.DarkHive;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(286, 255);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(97, 44);
            btnCancel.SoundType = SoundPalette.SoundType.Click;
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Location = new Point(109, 126);
            label1.Name = "label1";
            label1.Size = new Size(89, 18);
            label1.TabIndex = 6;
            label1.Text = "Login name:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Location = new Point(121, 187);
            label2.Name = "label2";
            label2.Size = new Size(79, 18);
            label2.TabIndex = 7;
            label2.Text = "Password:";
            // 
            // LoginForm
            // 
            AcceptButton = btnSubmit;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.DimGray;
            CancelButton = btnCancel;
            ClientSize = new Size(482, 331);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lbInfo);
            Controls.Add(tbLogin);
            Controls.Add(tbPassword);
            Controls.Add(btnCancel);
            Controls.Add(btnSubmit);
            ForeColor = Color.White;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Zero-K Login/Register";
            Load += LoginForm_Load;
            ResumeLayout(false);
            PerformLayout();


            //AllowTransparency = true;
            //TransparencyKey = Color.FromArgb(255, 255, 255, );

            var textBackColor = Color.FromArgb(255, 0, 100, 140);

            lbInfo.BackColor = textBackColor;
            label1.BackColor = textBackColor;
            label2.BackColor = textBackColor;

            tbLogin.Text = Program.Conf.LobbyPlayerName;
            if (string.IsNullOrEmpty(tbLogin.Text))
            {
                tbLogin.Text = Program.SteamHandler.SteamName;
                Program.SteamHandler.SteamHelper.SteamOnline += SteamApiOnSteamOnline;
            }
            tbPassword.Text = Program.Conf.LobbyPlayerPassword;
        }

        public string InfoText { set { lbInfo.Text = value; } }


        public string LoginValue { get { return tbLogin.Text; } }

        public string PasswordValue { get { return tbPassword.Text; } }

        protected override void OnPaintBackground(PaintEventArgs e) {
            var page = Program.MainWindow.navigationControl.CurrentNavigatable as Control;
            if (page?.BackgroundImage != null) this.RenderControlBgImage(page, e);
            else e.Graphics.Clear(Config.BgColor);
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
        }

        private void SteamApiOnSteamOnline() {
            Program.MainWindow.InvokeFunc(
                () =>
                {
                    if (string.IsNullOrEmpty(tbLogin.Text)) tbLogin.Text = Program.SteamHandler.SteamName;
                    Program.SteamHandler.SteamHelper.SteamOnline -= SteamApiOnSteamOnline;
                });
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }


        private void btnSubmit_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void LoginForm_Load(object sender, EventArgs e) {
            Icon = ZklResources.ZkIcon;
        }
    }
}