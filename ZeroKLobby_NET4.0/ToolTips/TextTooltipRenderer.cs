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
          
          using (var fbrush = new SolidBrush(foreColor)) g.DrawString(text, font, fbrush, new Rectangle(0,0, size.Width, size.Height));
        }

        public Size? GetSize(Font font)
        {
            return TextRenderer.MeasureText(text, font, new Size(300, 40), TextFormatFlags.WordBreak);
        }
    }
}