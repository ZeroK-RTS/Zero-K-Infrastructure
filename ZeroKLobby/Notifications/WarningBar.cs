// Contact: Jan Lichovník  licho@licho.eu, tel: +420 604 935 349,  www.itl.cz
// Last change by: licho  03.07.2016

using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroKLobby.Notifications
{
    public partial class WarningBar: ZklNotifyBar
    {
        private Action clickAction;
        protected WarningBar(string text, string buttonText, Action clickAction)
        {
            InitializeComponent();
            lbText.Font = Config.GeneralFont;
            lbText.Text = text;
            if (!string.IsNullOrEmpty(buttonText)) bitmapButton1.Text = buttonText;
            this.clickAction = clickAction;
        }

        public static WarningBar DisplayWarning(string text, string buttonText =null, Action buttonAction = null)
        {
            var bar = new WarningBar(text, buttonText, buttonAction);
            Program.NotifySection?.AddBar(bar);
            return bar;
        }

        private void bitmapButton1_Click(object sender, EventArgs e)
        {
            Program.NotifySection.RemoveBar(this);
            clickAction?.Invoke();
        }
    }
}