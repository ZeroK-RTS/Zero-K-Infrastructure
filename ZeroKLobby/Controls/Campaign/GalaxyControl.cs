using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby.Campaign
{
    public partial class GalaxyControl : UserControl
    {
        Bitmap background;

        public GalaxyControl()
        {
            InitializeComponent();
            BorderStyle = (BorderStyle)FrameBorderRenderer.StyleType.Shraka;
        }

        public void SetBackground(string background)
        {
            this.background = new Bitmap(background);
        }
    }
}
