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

namespace ZeroKLobby.Controls.Campaign
{
    public partial class PlanetInfoPanel : UserControl
    {
        public PlanetInfoPanel()
        {
            InitializeComponent();
        }

        public void SetParams(Planet planet)
        {
            planetPictureBox.BackgroundImage = (Bitmap)GalaxyResources.ResourceManager.GetObject(planet.Image);
            blurbWindow.ClearTextWindow();
            blurbWindow.AppendText(planet.Blurb ?? "No description... yet");
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }


    }
}
