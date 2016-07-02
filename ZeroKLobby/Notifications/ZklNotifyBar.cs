using System.Drawing;
using System.Windows.Forms;
using ZeroKLobby.Controls;

namespace ZeroKLobby.Notifications
{
    public class ZklNotifyBar : ZklBaseControl
    {
        public ZklNotifyBar()
        {
            Dock = DockStyle.Top;
            ForeColor = Config.TextColor;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.TechPanelHollow);
        }
    }
}