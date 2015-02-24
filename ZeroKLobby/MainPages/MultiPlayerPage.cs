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
    public partial class MultiPlayerPage : UserControl, IMainPage
    {
        public MultiPlayerPage()
        {
            InitializeComponent();
            btnCustomBattles.Font = Config.MenuFont;
            btnJoinQueue.Font = Config.MenuFont;
            btnSpectate.Font = Config.MenuFont;
        }


        public void GoBack()
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.Home);
        }

        public string Title { get { return "Multiplayer"; } }
        public Image MainWindowBgImage { get { return BgImages.blue_galaxy; } }

        private void btnCustomBattles_Click(object sender, EventArgs e)
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.CustomBattles,false);
        }
    }
}
