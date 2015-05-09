using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CampaignLib;
using ZeroKLobby.MainPages;
using ZeroKLobby.Campaign;
using ZeroKLobby.Controls.Campaign;

namespace ZeroKLobby.MicroLobby.Campaign
{
    public partial class CampaignPage : UserControl, IMainPage
    {
        CampaignManager manager;
        Dictionary<string, BitmapButton> buttons = new Dictionary<string, BitmapButton>();
        
        JournalPanel journalPanel;

        public CampaignPage()
        {
            InitializeComponent();
            manager = new CampaignManager();
            manager.LoadCampaign("test");
            manager.LoadCampaignSave("test", "save1");

            journalButton.Font = Config.MenuFont;
            commButton.Font = Config.MenuFont;
            saveButton.Font = Config.MenuFont;
            loadButton.Font = Config.MenuFont;

            journalButton.Click += (sender, eventArgs) =>
                {
                    if (journalPanel != null)
                    {
                        journalPanel.Dispose();
                    }
                    journalPanel = new JournalPanel();
                    journalPanel.Left = 240;  // FIXME don't hardcode this kind of thing!!
                    journalPanel.LoadJournalEntries(manager.GetCampaign().Journals, manager.GetSave().JournalProgress);
                    journalPanel.Parent = this;
                    journalPanel.BringToFront();
                };

            galControl.BackgroundImage = (Bitmap)GalaxyResources.ResourceManager.GetObject(manager.GetCampaign().Background);
            galControl.BackgroundImageLayout = ImageLayout.Stretch;
            galControl.SetPlanets(manager.GetVisiblePlanets());
        }

        public void GoBack()
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.SinglePlayer);
        }
        public string Title { get { return "Campaign"; } }
        public Image MainWindowBgImage { get { return BgImages.bg_battle; } }

        
    }
}
