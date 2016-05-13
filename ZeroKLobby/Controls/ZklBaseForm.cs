using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    public class ZklBaseControl: UserControl
    {
        public ZklBaseControl() {
            Init(this);
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
        }

        public static void Init(Control control) {
            control.Font = Config.GeneralFont;
            control.BackColor = Config.BgColor;
        }
    }

    public class ZklBaseForm: Form
    {
        public ZklBaseForm() {
            Init(this);
            DoubleBuffered = true;
        }

        public static void Init(Form form) {
            form.Font = Config.GeneralFont;
            form.BackColor = Config.BgColor;
            form.FormBorderStyle = FormBorderStyle.None;
            form.AutoScaleMode = AutoScaleMode.None;
        }
    }
}