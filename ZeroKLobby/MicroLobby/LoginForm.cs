using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
	public partial class LoginForm: Form
	{
		public string InfoText { set { lbInfo.Text = value; } }

		public string LoginValue {
            get
            {
                return tbLogin.Text;
            } 
        }

		public string PasswordValue { 
            get {
                return tbPassword.Text;
            }
        }

	    protected override void OnPaintBackground(PaintEventArgs e)
	    {
            //base.OnPaintBackground(e);
	        /*if (Environment.OSVersion.Platform != PlatformID.Unix) {
	            using (var br = new SolidBrush(Color.FromArgb(255,0,255,255))) {
	                e.Graphics.FillRectangle(br, e.ClipRectangle);
	            }
	        }*/
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.Shraka);
	    }


		public LoginForm()
		{
            Font = Config.GeneralFontBig;
            InitializeComponent();
		    //AllowTransparency = true;
		    //TransparencyKey = Color.FromArgb(255, 255, 255, );
		    

            var textBackColor = Color.FromArgb(255, 0, 100, 140);
		   
		    lbInfo.BackColor = textBackColor;
		    label1.BackColor = textBackColor;
		    label2.BackColor = textBackColor;

            tbLogin.Text = Program.Conf.LobbyPlayerName;
		    if (string.IsNullOrEmpty(tbLogin.Text)) {
		        tbLogin.Text = Program.SteamHandler.SteamName;
                Program.SteamHandler.SteamHelper.SteamOnline += SteamApiOnSteamOnline;
		    }
		    tbPassword.Text = Program.Conf.LobbyPlayerPassword;
        }

	    void SteamApiOnSteamOnline()
	    {
	        Program.MainWindow.InvokeFunc(() =>
	        {
	            if (string.IsNullOrEmpty(tbLogin.Text)) {
	                tbLogin.Text = Program.SteamHandler.SteamName;
	            }
	            Program.SteamHandler.SteamHelper.SteamOnline -= SteamApiOnSteamOnline;
	        });
	    }

	    void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
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