using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    public class ZklBaseControl:UserControl
    {
        public ZklBaseControl():base() {
            Font = Config.GeneralFont;
            BackColor = Config.BgColor;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
        }
    }

    public class ZklBaseForm: Form
    {
        public ZklBaseForm() : base()
        {
            Font = Config.GeneralFont;
            BackColor = Config.BgColor;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.None;
        }
    }

}
