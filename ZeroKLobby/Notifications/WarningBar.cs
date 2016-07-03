// Contact: Jan Lichovník  licho@licho.eu, tel: +420 604 935 349,  www.itl.cz
// Last change by: licho  03.07.2016

using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroKLobby.Notifications
{
    public partial class WarningBar: ZklNotifyBar
    {
        protected WarningBar(string text)
        {
            InitializeComponent();
            lbText.Font = Config.GeneralFont;
            lbText.Text = text;
        }

        public static WarningBar DisplayWarning(string text)
        {
            var bar = new WarningBar(text);

            Program.NotifySection.AddBar(bar);
            return bar;
        }

        private void bitmapButton1_Click(object sender, EventArgs e)
        {
            Program.NotifySection.RemoveBar(this);
        }
    }
}