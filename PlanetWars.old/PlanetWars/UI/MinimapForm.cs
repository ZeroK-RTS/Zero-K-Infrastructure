using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PlanetWars.UI
{
    class MinimapForm : Form
    {
        public MinimapForm(Map map)
        {
            if (map == null) {
                return;
            }
            base.Text = "Review Selected Map";
            ShowIcon = false;
            Width = 600;
            Height = 500;
            var mapButton = new ToolStripButton("Map Information");
            var minimapButton = new ToolStripButton("Minimap");
            var mapInfoView = new MapInfoView(map);
            var minimapBox = new MinimapBox(map, 1);
            var heightmapButton = new ToolStripButton("Heightmap");
            var heightmapBox = new PictureBox
            {Image = map.HeightMap, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black,};
            var metalmapButton = new ToolStripButton("Metalmap");
            var metalmapBox = new PictureBox
            {Image = map.MetalMap, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black,};
            TabbedControls =
                new TabbedControls(
                    new Dictionary<ToolStripButton, System.Windows.Forms.Control>
                    {
                        {minimapButton, minimapBox},
                        {heightmapButton, heightmapBox},
                        {metalmapButton, metalmapBox},
                        {mapButton, mapInfoView},
                    });
            TabbedControls.ToolTabs.Dock = DockStyle.Top;
            TabbedControls.OkCancelBar.OK += OkCancelBar_OK;
            TabbedControls.OkCancelBar.Cancel += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(TabbedControls);
        }

        protected TabbedControls TabbedControls;

        void OkCancelBar_OK(object sender, EventArgs e)
        {
            if (DialogResult.Yes ==
                MessageBox.Show(
                    "Are you sure you want to irreversibly select this map?", "Select Map", MessageBoxButtons.YesNo)) {
                DialogResult = DialogResult.OK;
            }
        }
    }
}