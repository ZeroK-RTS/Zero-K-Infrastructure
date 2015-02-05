using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public partial class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            InitializeComponent();

            DoubleBuffered = true;

            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackgroundImage = ZklResources.bg_battle;
            this.BackgroundImageLayout = ImageLayout.Stretch;


            var avatar = new BitmapButton() { Size = new Size(75, 75), Left = 50, Top = 100, Image = ZklResources.grayuser};

            Controls.Add(avatar);
            Controls.Add(new BitmapButton() { Size = new Size(75, 30), Text = "Login", Left = 50, Top = 180 });
            Controls.Add(new Label() {Text = "Licho", Left = 130, Top = 120, AutoSize = true, Font = new Font("Verdana",25), BackColor = Color.Transparent ,ForeColor = Color.White});

            
            Controls.Add(new BitmapButton() { Size = new Size(250, 50), Text = "SinglePlayer", Left = 50, Top = 350});
            Controls.Add(new BitmapButton() { Size = new Size(250, 50), Text = "MultiPlayer", Left = 50, Top = 450});
            Controls.Add(new BitmapButton() { Size = new Size(250, 50), Text = "Exit", Left = 50, Top = 550 });

            var winButton = new BitmapButton() {
                Size = new Size(50, 50),
                Text = "WIN",
                Left = 50,
                Top = Height - 100,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            winButton.Click += (sender, args) => {
                if (FormBorderStyle == FormBorderStyle.None) {
                    TopMost = false;
                    WindowState = FormWindowState.Normal;
                    FormBorderStyle = FormBorderStyle.Sizable;
                } else {
                    FormBorderStyle = FormBorderStyle.None;
                    TopMost = true;
                    WindowState = FormWindowState.Maximized;
                }
            };
            
            Controls.Add(winButton);

            Controls.Add(new BitmapButton() { Size = new Size(50, 50), Text = "SND", Left = 120, Top = Height - 100, Anchor = AnchorStyles.Bottom | AnchorStyles.Left });

        }

        protected override void OnDeactivate(EventArgs e)
        {
            this.TopMost = false;
            base.OnDeactivate(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            this.TopMost = true;
            base.OnActivated(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }
    }
}
