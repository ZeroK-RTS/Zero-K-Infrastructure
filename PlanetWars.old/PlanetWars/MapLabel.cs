using System.Drawing;
using PlanetWars.Utility;
using PlanetWarsShared;

namespace PlanetWars
{
    public class MapLabel : IDrawable
    {
        protected MapLabel()
        {
            Brush = new SolidBrush(Color.White);
            InvertedBrush = new SolidBrush(Brush.Color.Invert());
            OutlineSize = 3;
        }

        public MapLabel(int outlineSize,
                        SolidBrush brush,
                        Font font,
                        SolidBrush invertedBrush,
                        PointF position,
                        string text)
        {
            OutlineSize = outlineSize;
            Brush = brush;
            Font = font;
            InvertedBrush = invertedBrush;
            Position = position;
            Text = text;
        }

        public MapLabel(string text, PointF position, Font font) : this()
        {
            Text = text;
            Position = position;
            Font = font;
        }

        public MapLabel(string text, PointF position) : this(text, position, new Font("Tahoma", 10)) {}

        public MapLabel(PointF position) : this()
        {
            Position = position;
            Font = new Font("Tahoma", 10);
        }

        public int OutlineSize { get; set; }
        public SolidBrush Brush { get; set; }
        public Font Font { get; set; }
        public SolidBrush InvertedBrush { get; set; }

        public bool Selected { get; set; }

        public PointF Position { get; set; }
        public string Text { get; set; }

        #region IDrawable Members

        public void Draw(Graphics g, Size mapSize)
        {
            if (Text == null) {
                return;
            }
            Point point = Position.Scale(mapSize).Translate(5, 5).ToPoint();
            for (int x = 0; x < OutlineSize; x++) {
                for (int y = 0; y < OutlineSize; y++) {
                    g.DrawString(
                        Text,
                        Font,
                        Selected ? Brush : InvertedBrush,
                        new Point(point.X + x - OutlineSize/2, point.Y + y - OutlineSize/2));
                }
            }

            g.DrawString(Text, Font, Selected ? InvertedBrush : Brush, point);
        }

        #endregion

        #region disabled code

#if false // Makes the outline pixellated
        Bitmap DrawTextToBitmap(bool inverted)
        {
            Size textSize = TextRenderer.MeasureText(text, font);
            Bitmap bitmap = new Bitmap(textSize.Width, textSize.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                DrawText(g, Point.Empty, inverted);
            }
            return bitmap;
        }
#endif

        #endregion
    }
}