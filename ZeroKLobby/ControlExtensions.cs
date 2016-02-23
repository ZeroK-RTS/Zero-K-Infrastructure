using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby
{
    static class ControlExtensions
    {
        public static bool IsInDesignMode(this Control Control)
        {
            // workaround for Control.DesignMode not working in constructor
            return Process.GetCurrentProcess().ProcessName == "devenv";
        }
    }
}
