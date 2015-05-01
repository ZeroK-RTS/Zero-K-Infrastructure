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
        List<Planet> planetsToRender = new List<Planet>();
        Dictionary<string, BitmapButton> buttons = new Dictionary<string, BitmapButton>();
        PlanetInfoPanel planetInfoPanel;

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

            galControl.BackgroundImage = (Bitmap)GalaxyResources.ResourceManager.GetObject(manager.GetCampaign().Background);
            galControl.BackgroundImageLayout = ImageLayout.Stretch;

            planetsToRender = manager.GetUnlockedPlanets();
            foreach (Planet planet in planetsToRender)
            {
                Button button = new Button();
                button.Parent = this.galControl;
                button.Width = 48;
                button.Height = 48;
                button.Location = new Point((int)planet.X, (int)planet.Y);
                button.BackgroundImage = (Bitmap)GalaxyResources.ResourceManager.GetObject(planet.Image);
                button.BackgroundImageLayout = ImageLayout.Stretch;
                button.BackColor = Color.Transparent;
                button.ForeColor = Color.Transparent;
                button.Click += (sender, eventArgs) =>
                    {
                        if (planetInfoPanel != null)
                        {
                            planetInfoPanel.Dispose();
                        }
                        planetInfoPanel = new PlanetInfoPanel();
                        planetInfoPanel.Parent = galControl;
                        planetInfoPanel.SetParams(planet);
                    };
            }
        }

        public void GoBack()
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.SinglePlayer);
        }
        public string Title { get { return "Campaign"; } }
        public Image MainWindowBgImage { get { return BgImages.bg_battle; } }

        
    }
}
