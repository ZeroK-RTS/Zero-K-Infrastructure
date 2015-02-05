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
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.WindowState = FormWindowState.Maximized;
            this.BackgroundImage = ZklResources.bg_battle;
            this.BackgroundImageLayout = ImageLayout.Zoom;
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
