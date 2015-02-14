using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZeroKLobby.MicroLobby.ExtrasTab;
using ZkData;

namespace ZeroKLobby.MainPages
{
    public partial class SinglePlayerPage : UserControl
    {
        public SinglePlayerPage()
        {
            InitializeComponent();
        }

        private void tutorialButton_Click(object sender, EventArgs e)
        {
            Program.BrowserInterop.OpenUrl(string.Format("{0}//Missions/", GlobalConst.BaseSiteUrl));
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.Home);
        }

        private void missonsButton_Click(object sender, EventArgs e)
        {
            Program.BrowserInterop.OpenUrl(string.Format("{0}//Missions/", GlobalConst.BaseSiteUrl));
        }

        private void campaignButton_Click(object sender, EventArgs e)
        {
            Program.BrowserInterop.OpenUrl(string.Format("{0}//Campaign/", GlobalConst.BaseSiteUrl));
        }

        private void skirmishButton_Click(object sender, EventArgs e)
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.Skirmish, false);
        }
    }
}
