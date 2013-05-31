using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby
{
    class ZkSplitContainer: SplitContainer, ISupportInitialize
    {
        void ISupportInitialize.BeginInit() {
            //if (Environment.OSVersion.Platform != PlatformID.Unix) base.BeginInit();
        }
        void ISupportInitialize.EndInit() {
            //if (Environment.OSVersion.Platform != PlatformID.Unix) base.EndInit();

        }
    }
}
