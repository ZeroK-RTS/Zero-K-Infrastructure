using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PlanetWars.UI
{
    class PlanetInfoForm : Form
    {
        public PlanetInfoForm(PlanetDrawing planet, bool changeMap, bool changeName)
        {
            base.Text = "Planet Information";
            ShowIcon = false;
            Width = 600;
            Height = 500;
            var mapButton = new ToolStripButton("Map Information");
            var minimapButton = new ToolStripButton("Minimap");
            var mapInfoView = new MapInfoView(planet.Map);
            var minimapBox = new MinimapBox(planet.Map, 1);
            var heightmapButton = new ToolStripButton("Heightmap");
            var heightmapBox = new PictureBox
            {
                Image = planet.Map.HeightMap,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
            };
            var metalmapButton = new ToolStripButton("Metalmap");
            var metalmapBox = new PictureBox
            {
                Image = planet.Map.MetalMap,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
            };
            var dict = new Dictionary<ToolStripButton, System.Windows.Forms.Control>
            {
                {minimapButton, minimapBox},
                {heightmapButton, heightmapBox},
                {metalmapButton, metalmapBox},
                {mapButton, mapInfoView},
            };
            if (changeMap) {
                dict.Add(new ToolStripButton("Change Map"), new ChangeMapControl());
            }
            if (changeName) {
                dict.Add(new ToolStripButton("Change Name"), new ChangeNameControl());
            }
            var tabbedControls = new TabbedControls(dict);
            tabbedControls.OkCancelBar.Visible = false;
            tabbedControls.ToolTabs.Dock = DockStyle.Top;
            Controls.Add(tabbedControls);
        }
    }
}