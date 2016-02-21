using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZeroKLobby
{
    public class MapTooltipRenderer: IToolTipRenderer
    {
        Map map;
        string mapName;

        public void SetMapTooltipRenderer(string mapName)
        {
            this.mapName = mapName;
            Program.SpringScanner.MetaData.GetMapAsync(mapName, (map, minimap, heightmap, metalmap) => { this.map = map; }, (exc) => { });
        }

        public void Draw(Graphics g, Font font, Color foreColor)
        {
            var x = 1;
            var y = 0;
            var fbrush = new SolidBrush(foreColor);

            Action newLine = () =>
                {
                    x = 1;
                    y += 16;
                };
            Action<string> drawString = text =>
                {
                    g.DrawString(text, font, fbrush, new Point(x, y));
                    x += (int)Math.Ceiling((double)g.MeasureString(text, font).Width);
                };
            Action<Image, int, int> drawImage = (image, w, h) =>
                {
                    g.DrawImage(image, x, y, w, h);
                    x += w + 3;
                };
            using (var boldFont = new Font(font, FontStyle.Bold)) g.DrawString(Map.GetHumanName(mapName), boldFont, fbrush, new Point(x, y));

            if (map != null)
            {
                newLine();
                //drawString(map.HumanName);
                //newLine();

                drawString(string.Format("Size: {0}x{1}", map.Size.Width/512, map.Size.Height/512));
                newLine();

                drawString(string.Format("Wind: {0} - {1}", map.MinWind, map.MaxWind));
                newLine();

                drawString("Gravity: " + map.Gravity);
                newLine();

                drawString("Tidal: " + map.TidalStrength);
                newLine();

                drawString("Max metal: " + map.MaxMetal);
                newLine();

                drawString("Extractor radius: " + map.ExtractorRadius);
                newLine();

                foreach (var line in map.Description.Lines())
                {
                    drawString(line);
                    newLine();
                }
            }

            if (Program.TasClient.MyBattle != null && !String.IsNullOrEmpty(Program.ModStore.ChangedOptions))
            {
                newLine();
                drawString("Game Options:");
                newLine();
                foreach (var line in Program.ModStore.ChangedOptions.Lines().Where(z => !string.IsNullOrEmpty(z)))
                {
                    drawString("  " + line);
                    newLine();
                }
            }
            fbrush.Dispose();
        }

        public Size? GetSize(Font font)
        {
            var h = 0;
            h += 16; //name
            if (map != null) h += 16*6 + map.Description.Lines().Length*16;

            // mod options
            if (Program.TasClient.MyBattle != null && !String.IsNullOrEmpty(Program.ModStore.ChangedOptions))
            {
                h += 16; // blank line
                h += 16; // title
                h += Program.ModStore.ChangedOptions.Lines().Where(x => !string.IsNullOrEmpty(x)).Count()*16;
            }

            return new Size(300, h);
        }
    }
}