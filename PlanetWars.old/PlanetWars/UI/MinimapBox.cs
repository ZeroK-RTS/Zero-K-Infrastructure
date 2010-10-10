using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PlanetWars.UI
{
    class MinimapBox : System.Windows.Forms.Control
    {
        readonly Font font = new Font("Tahoma", 10);
        Map map;
        readonly PictureBox pictureBox = new PictureBox {Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom};

        public Map Map
        {
            get { return map; }
            set
            {
                map = value;
                pictureBox.Image = value.Minimap;
                Invalidate();
                Update();
            }
        }

        public MinimapBox(Map map, float size)
        {
            this.map = map;
            int width = (int)(map.Minimap.Width*size);
            int height = (int)(map.Minimap.Height*size);
            Bitmap image = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(image)) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(map.Minimap, new Rectangle(Point.Empty, image.Size));
            }
            base.Dock = DockStyle.Fill;
            pictureBox.Width = Width = width;
            pictureBox.Height = Height = height;
            pictureBox.Image = image;
            pictureBox.Paint += pictureBox_Paint;
            pictureBox.BackColor = Color.Black;
            Controls.Add(pictureBox);
        }

        void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            float mapRatio = (float)map.Size.Width/map.Size.Height;
            float clientRatio = (float)pictureBox.Width/pictureBox.Height;
            float scaleX = mapRatio > clientRatio ? 1 : mapRatio/clientRatio;
            float scaleY = mapRatio > clientRatio ? clientRatio/mapRatio : 1;
            int offsetX = (int)((1 - scaleX)/2*pictureBox.Width);
            int offsetY = (int)((1 - scaleY)/2*pictureBox.Height);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            var format = new StringFormat {Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center};
            for (int i = 0; i < map.Positions.Length; i++) {
                var point = new PointF(
                    map.Positions[i].X/(float)map.Size.Width, map.Positions[i].Y/(float)map.Size.Height);
                if (point.X <= 1 && point.Y <= 1) {
                    var imagePoint = new Point(
                        (int)(point.X*pictureBox.Width*scaleX) + offsetX,
                        (int)(point.Y*pictureBox.Height*scaleY) + offsetY);
                    e.Graphics.FillEllipse(Brushes.Black, imagePoint.X - 10, imagePoint.Y - 10, 20, 20);
                    e.Graphics.DrawString(i.ToString(), font, Brushes.White, imagePoint.X, imagePoint.Y, format);
                }
            }
        }
    }
}