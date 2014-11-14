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
            DpiMeasurement.DpiXYMeasurement();
            var y = DpiMeasurement.ScaleValueY(21); 
            Region = new Region(new Rectangle(4, y + 1, Width - 8, Height - y - 4)); //Note: margin is not effected by DPI scaling
        }

        protected override void OnClick(EventArgs e) {
        }

    }
}
