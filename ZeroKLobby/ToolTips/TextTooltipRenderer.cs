using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby
{
    internal class TextTooltipRenderer: IToolTipRenderer
    {
        private string text;

        public void Draw(Graphics g, Font font, Color foreColor) {
            var size = (Size)GetSize(font);

            using (var fbrush = new SolidBrush(foreColor)) g.DrawString(text, font, fbrush, new Rectangle(0, 0, size.Width, size.Height));
        }

        public Size? GetSize(Font font) {
            var size = TextRenderer.MeasureText(text, font, new Size(300, 40), TextFormatFlags.WordBreak);
            size.Width += 5; // silly hack for measuretext vs drawstring slight mismatch in sizes (missing last letter in tooltip)
            size.Height += 5;
            return size;
        }

        public void SetTextTooltipRenderer(string text) {
            this.text = text;
        }
    }
}