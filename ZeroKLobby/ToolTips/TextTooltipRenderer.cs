using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby
{
    internal class TextTooltipRenderer: IToolTipRenderer
    {
        private string text;

        public void Draw(Graphics g, Font font, Color foreColor) {
            var size = (Size)GetSize(font);
            TextRenderer.DrawText(g, text, font, new Rectangle(0, 0, size.Width + 20, size.Height + 20), foreColor, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        }

        public Size? GetSize(Font font) {
            var size = TextRenderer.MeasureText(text, font, new Size(300, 40), TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
            return size;
        }

        public void SetTextTooltipRenderer(string text) {
            this.text = text;
        }
    }
}