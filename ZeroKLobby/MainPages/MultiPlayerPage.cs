using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby.MainPages
{
    public partial class MultiPlayerPage : UserControl
    {
        public MultiPlayerPage()
        {
            InitializeComponent();
        }

        private void bitmapButton1_Click(object sender, EventArgs e)
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.Home);
        }
    }
}
