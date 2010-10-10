using System.Drawing;

namespace SpringDownloader
{
    public interface IToolTipRenderer
    {
        void Draw(Graphics g, Font font, Color foreColor);
        Size? GetSize(Font font);
    }
}