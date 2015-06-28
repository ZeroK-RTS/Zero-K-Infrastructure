using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby
{
    class TextTooltipRenderer: IToolTipRenderer
    {
        string text;

        public void SetTextTooltipRenderer(string text)
        {
            this.text = text;
        }

        public void Draw(Graphics g, Font font, Color foreColor)
        {
          var size = (Size)GetSize(font);  
          
          TextRenderer.DrawText(g, text, font,new Rectangle(0,0, size.Width, size.Height), foreColor, TextFormatFlags.WordBreak);
        }

        public Size? GetSize(Font font)
        {
            return TextRenderer.MeasureText(text, font, new Size(300, 40), TextFormatFlags.WordBreak);
        }
    }
}