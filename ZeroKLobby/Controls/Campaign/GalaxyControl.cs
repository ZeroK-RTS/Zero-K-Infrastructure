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
using ZeroKLobby.Controls.Campaign;

namespace ZeroKLobby.Campaign
{
    public partial class GalaxyControl : UserControl
    {
        Bitmap background;
        List<Planet> planetsToRender = new List<Planet>();
        List<Button> planetButtons = new List<Button>();
        Dictionary<Planet, Planet> connections = new Dictionary<Planet, Planet>();
        PlanetInfoPanel planetInfoPanel;

        public GalaxyControl()
        {
            InitializeComponent();
            BorderStyle = (BorderStyle)FrameBorderRenderer.StyleType.Shraka;
        }

        public void SetPlanets(List<Planet> planets)
        {
            planetsToRender = planets;
            planetButtons = new List<Button>();
            connections = new Dictionary<Planet, Planet>();

            foreach (Planet planet in planets)
            {
                Button button = new Button();
                button.Parent = this;
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
                    planetInfoPanel.Parent = this;
                    planetInfoPanel.SetParams(planet);
                };
                planetButtons.Add(button);
                foreach (string linkedPlanetID in planet.LinkedPlanets)
                {
                    Planet linkedPlanet = planets.FirstOrDefault(x => x.ID == linkedPlanetID);
                    if (linkedPlanet != null) connections.Add(planet, linkedPlanet);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            using (Pen myPen = new Pen(Color.White))
            {
                myPen.Width = 4;
                myPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                myPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                foreach (var connection in connections)
                {
                    Planet planet1 = connection.Key;
                    Planet planet2 = connection.Value;
                    g.DrawLine(myPen, planet1.X, planet1.Y, planet2.X, planet2.Y);
                }
            }
        }

        public void SetBackground(string background)
        {
            this.background = new Bitmap(background);
        }
    }
}
