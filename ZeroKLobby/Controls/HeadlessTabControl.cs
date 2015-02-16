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
           SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer,true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            SetRegion();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            
            SetRegion();
        }

        void SetRegion()
        {
            Region = new Region(new Rectangle(4, this.ItemSize.Height + 4, Width - 8, Height - this.ItemSize.Height - 8));
        }
    }
}
