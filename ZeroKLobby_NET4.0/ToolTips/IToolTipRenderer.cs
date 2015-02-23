using System.Drawing;

namespace ZeroKLobby
{
    public interface IToolTipRenderer
    {
        void Draw(Graphics g, Font font, Color foreColor);
        Size? GetSize(Font font);
    }
}