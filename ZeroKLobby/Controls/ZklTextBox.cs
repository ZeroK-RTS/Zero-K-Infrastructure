using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    internal class ZklTextBox: TextBox
    {
        public ZklTextBox() {
            ZklBaseControl.Init(this);

            BorderStyle = BorderStyle.None;
            ForeColor = Config.TextColor;
            BackColor = Color.Transparent;
            Font = Config.GeneralFontSmall;
            SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e) {
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.Red, ButtonBorderStyle.Solid);
            base.OnPaint(e);
        }
    }
}