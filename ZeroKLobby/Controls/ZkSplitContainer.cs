using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public class ZkSplitContainer: SplitContainer, ISupportInitialize
    { 
        //NOTE: this didn't fix problem in Linux. It still say ISupportInitialize not implemented.
        //"A System.Windows.Forms.SplitContainer doesn't implement interface System.ComponentModel.ISupportInitialize"
        void ISupportInitialize.BeginInit() {
            //if (Environment.OSVersion.Platform != PlatformID.Unix) base.BeginInit();
        }
        void ISupportInitialize.EndInit() {
            //if (Environment.OSVersion.Platform != PlatformID.Unix) base.EndInit();

        }
    }
}
