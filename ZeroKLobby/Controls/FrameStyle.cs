using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZeroKLobby
{
    
    public class FrameStyle
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
    }
}