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
        List<PlanetConnection> connections = new List<PlanetConnection>();
        PlanetInfoPanel planetInfoPanel;

        public GalaxyControl()
        {
            InitializeComponent();
            BorderStyle = (BorderStyle)FrameBorderRenderer.StyleType.Shraka;
        }

        public void SetPlanets(List<Planet> planets)
        {
            foreach (Button button in planetButtons)
            {
                button.Dispose();
            }

            planetsToRender = planets;
            planetButtons = new List<Button>();
            connections = new List<PlanetConnection>();

            foreach (Planet planet in planets)
            {
                Button button = new Button();
                button.Parent = this;
                button.Width = (int)planet.Size;
                button.Height = (int)planet.Size;
                button.Location = new Point((int)(planet.X), (int)(planet.Y));
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
                    if (linkedPlanet != null) connections.Add(new PlanetConnection(planet, linkedPlanet));
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            using (Pen myPen = new Pen(Color.White))
            {
                myPen.Width = 2;
                myPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                myPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                foreach (var connection in connections)
                {
                    Planet planet1 = connection.planet1;
                    Planet planet2 = connection.planet2;
                    g.DrawLine(myPen, (int)planet1.X, (int)planet1.Y, (int)planet2.X, (int)planet2.Y);
                }
            }
        }

        public void SetBackground(string background)
        {
            this.background = new Bitmap(background);
        }

        public class PlanetConnection
        {
            public Planet planet1 { get; protected set; }
            public Planet planet2 { get; protected set; }

            public PlanetConnection(Planet planet1, Planet planet2)
            {
                this.planet1 = planet1;
                this.planet2 = planet2;
            }
        }
    }
}
