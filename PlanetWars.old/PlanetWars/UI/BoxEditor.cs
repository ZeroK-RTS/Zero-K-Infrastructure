using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlanetWars.Utility;

namespace PlanetWars.UI
{
    class BoxEditor : Form
    {
        readonly GalaxyLoader loader = new GalaxyLoader(true);

        PictureBox heightmapBox;
        int index;
        PictureBox metalmapBox;
        MinimapBox minimapBox;

        public BoxEditor()
        {
            loader.RunWorkerCompleted += loader_RunWorkerCompleted;
            loader.RunWorkerAsync();
        }

        void loader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ReloadMaps();
            Map map = GalaxyMap.Instance.Maps[index];
            base.Text = "Box Editor";
            ShowIcon = false;
            Width = 600;
            Height = 500;
            var mapButton = new ToolStripButton("Map Information");
            var minimapButton = new ToolStripButton("Minimap");
            var mapInfoView = new MapInfoView(map);
            minimapBox = new MinimapBox(map, 1);
            var heightmapButton = new ToolStripButton("Heightmap");
            heightmapBox = new PictureBox
            {Image = map.HeightMap, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black,};
            var metalmapButton = new ToolStripButton("Metalmap");
            metalmapBox = new PictureBox
            {Image = map.MetalMap, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black,};
            var dict = new Dictionary<ToolStripButton, System.Windows.Forms.Control>
            {
                {minimapButton, minimapBox},
                {heightmapButton, heightmapBox},
                {metalmapButton, metalmapBox},
                {mapButton, mapInfoView},
            };
            var tabbedControls = new TabbedControls(dict);
            tabbedControls.OkCancelBar.Visible = false;
            tabbedControls.ToolTabs.Dock = DockStyle.Top;
            Controls.Add(tabbedControls);
            var nextMapButton = new ToolStripButton("Next");
            var previousMapButton = new ToolStripButton("Previous");
            tabbedControls.ToolTabs.Items.AddRange(new ToolStripItem[] {previousMapButton, nextMapButton});
            nextMapButton.Click += delegate
            {
                index++;
                SetMap();
            };
            previousMapButton.Click += delegate
            {
                index++;
                SetMap();
            };
        }

        void SetMap()
        {
            index = index.Constrain(0, GalaxyMap.Instance.Maps.Count - 1);
            var map = GalaxyMap.Instance.Maps[index];
            heightmapBox.Image = map.HeightMap;
            metalmapBox.Image = map.MetalMap;
            minimapBox.Map = map;
        }

        void ReloadMaps()
        {
            foreach (var mapName in GalaxyMap.Instance.Galaxy.MapNames) {
                if (!GalaxyMap.Instance.Maps.Any(p => p.Name == mapName)) {
                    var map = Map.FromDisk(mapName);
                    if (map != null) {
                        GalaxyMap.Instance.Maps.Add(map);
                    }
                }
            }
        }
    }
}