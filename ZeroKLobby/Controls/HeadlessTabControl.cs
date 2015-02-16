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
            SetRegion();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            SetRegion();
        }

        void SetRegion()
        {
            Region = new Region(new Rectangle(4, this.ItemSize.Height + 4, Width - 8, Height - this.ItemSize.Height - 8));
                //Note: the margin (+-4) doesn't seem to be effected by DPI scaling
        }
    }
}
