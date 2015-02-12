using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZeroKLobby
{
    public class ButtonStyle
    {
        public Image NW;
        public Image NE;
        public Image N;
        public Image SW;
        public Image SE;
        public Image S;
        public Image E;
        public Image W;

        public Size FillOffsetTopLeft = Size.Empty;
        public Size FillOffsetBottomRight = Size.Empty;
        public Brush FillBrush = Brushes.Transparent;
        
        public static ButtonStyle DarkHiveHoverStyle = new ButtonStyle() { N = DarkHiveHover.N, NE = DarkHiveHover.NE, NW = DarkHiveHover.NW, S = DarkHiveHover.S, SE = DarkHiveHover.SE, SW = DarkHiveHover.SW, E = DarkHiveHover.E, W = DarkHiveHover.W, FillBrush = new SolidBrush(Color.FromArgb(89,23, 252, 255)) };

        public static ButtonStyle DarkHiveStyle = new ButtonStyle() { N = DarkHive.N, NE = DarkHive.NE, NW = DarkHive.NW, S = DarkHive.S, SE = DarkHive.SE, SW = DarkHive.SW, E = DarkHive.E, W = DarkHive.W, FillBrush = new SolidBrush(Color.FromArgb(179, 0, 0, 0)) };

        public static ButtonStyle ShrakaStyle = new ButtonStyle() { N = Shraka.N, NE = Shraka.NE, NW = Shraka.NW, S = Shraka.S, SE = Shraka.SE, SW = Shraka.SW, E = Shraka.E, W = Shraka.W, FillBrush = new LinearGradientBrush(new Rectangle(0, 0, 1, 1), Color.FromArgb(166, 0, 44, 61), Color.FromArgb(166, 0, 104, 141), 90), FillOffsetTopLeft = new Size(-162, -78), FillOffsetBottomRight = new Size(-174, -98) };
    }
}