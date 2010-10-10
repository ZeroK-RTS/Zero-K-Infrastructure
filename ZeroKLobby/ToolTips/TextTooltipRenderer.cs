using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby
{
    class TextTooltipRenderer: IToolTipRenderer
    {
        string text;

        public TextTooltipRenderer(string text)
        {
            this.text = text;
        }

        public void Draw(Graphics g, Font font, Color foreColor)
        {
            TextRenderer.DrawText(g, text, font, new Point(0, 0), foreColor, TextFormatFlags.WordBreak);
        }

        public Size? GetSize(Font font)
        {
            return TextRenderer.MeasureText(text, font, new Size(300, 40), TextFormatFlags.WordBreak);
        }
    }
}