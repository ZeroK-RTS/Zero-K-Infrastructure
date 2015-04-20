using System;
using System.Drawing;
using System.Windows.Forms;
using ZkData;

namespace ZeroKLobby.MainPages
{
    
    public partial class SinglePlayerPage : UserControl, IMainPage
    {
        public SinglePlayerPage()
        {
            InitializeComponent();
            tutorialButton.Font = Config.MenuFont;
            campaignButton.Font = Config.MenuFont;
            missonsButton.Font = Config.MenuFont;
            skirmishButton.Font = Config.MenuFont;

            tutorialButton.Image = Buttons.link.GetResizedWithCache(32, 32);
            campaignButton.Image = Buttons.link.GetResizedWithCache(32, 32);
            missonsButton.Image = Buttons.link.GetResizedWithCache(32, 32);
        }

        private void tutorialButton_Click(object sender, EventArgs e)
        {
            Program.BrowserInterop.OpenUrl(string.Format("{0}//Missions/", GlobalConst.BaseSiteUrl));
        }

        private void missonsButton_Click(object sender, EventArgs e)
        {
            Program.BrowserInterop.OpenUrl(string.Format("{0}//Missions/", GlobalConst.BaseSiteUrl));
        }

        private void campaignButton_Click(object sender, EventArgs e)
        {
            //Program.BrowserInterop.OpenUrl(string.Format("{0}//Campaign/", GlobalConst.BaseSiteUrl));
            Program.MainWindow.SwitchPage(MainWindow.MainPages.Campaign, false);
        }

        private void skirmishButton_Click(object sender, EventArgs e)
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.Skirmish, false);
        }

        public void GoBack()
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.Home);
        }

        public string Title { get { return "Singleplayer"; } }

        public Image MainWindowBgImage { get { return BgImages.bg_battle; } }
    }
}
