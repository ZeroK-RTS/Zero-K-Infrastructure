using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    public class TransparentTabPage: TabPage
    {
        public TransparentTabPage()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            this.RenderParentsBackgroundImage(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
        }
    }
}
