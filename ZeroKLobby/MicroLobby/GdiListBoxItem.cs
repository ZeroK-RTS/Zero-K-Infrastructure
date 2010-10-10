using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    abstract class GdiListBoxItem
    {
        protected int height = 16;
        protected int y;

        public event MouseEventHandler Click = delegate { };

        public abstract void Draw(Graphics g, ref int y, int scrollPosition);

        public bool HitTest(int y)
        {
            return this.y < y && y < this.y + height;
        }

        public void OnClick(MouseEventArgs e)
        {
            if (HitTest(e.Location.Y)) Click(this, e);
        }
    }
}