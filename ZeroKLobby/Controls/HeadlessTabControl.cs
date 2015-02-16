using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public class HeadlessTabControl:TabControl
    {
        public HeadlessTabControl()
        {
           SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint,true);
            SetRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            SetRegion();
            base.OnPaint(e);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            SetRegion();
            base.OnPaintBackground(pevent);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            SetRegion();
        }

        void SetRegion()
        {
            Region = new Region(new Rectangle(4, this.ItemSize.Height + 4, Width - 8, Height - this.ItemSize.Height - 8));
        }
    }
}
