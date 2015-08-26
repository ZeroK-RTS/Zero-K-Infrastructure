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
using ZeroKLobby.MicroLobby.Campaign;

namespace ZeroKLobby.Controls.Campaign
{
    public partial class PlanetInfoPanel : UserControl
    {
        Planet planet;

        public PlanetInfoPanel()
        {
            InitializeComponent();
            planetNameBox.Font = Config.MenuFont;
        }

        public void SetParams(Planet planet)
        {
            this.planet = planet;
            planetNameBox.AppendText(planet.Name);
            planetImageBox.BackgroundImage = (Bitmap)GalaxyResources.ResourceManager.GetObject(planet.Image);
            planetBlurbBox.AppendText(planet.Blurb ?? "No description... yet");

            var missions = planet.Missions;
            foreach (Mission mission in missions)
            {
                if (!CampaignManager.IsMissionUnlocked(mission)) continue;
                BitmapButton button = new BitmapButton();
                button.Text = String.Format("{1}{0}", mission.Name, mission.IsMainQuest ? "" : "OPTIONAL: ");
                //button.Left = 4;
                button.AutoSize = true;
                button.Parent = missionFlowLayout;
                button.Width = button.Parent.Width - 8; // FIXME donut hardcode
                button.Click += (sender, eventArgs) => ((CampaignPage)this.Parent.Parent).EnterMission(mission);
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }


    }
}
