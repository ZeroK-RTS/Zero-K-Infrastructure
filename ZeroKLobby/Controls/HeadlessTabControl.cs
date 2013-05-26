using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby
{
    class HeadlessTabControl:TabControl
    {
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Region= new Region(new Rectangle(0,22, Width, Height));
        }

        protected override void OnClick(EventArgs e) {
        }

    }
}
