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

        public virtual string Title { get; set; }

        public virtual string TitleTooltip { get; set; }

        //public virtual BitmapButton btnDetail { get; set; } = new BitmapButton();

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, Bounds, FrameBorderRenderer.StyleType.TechPanelHollow);
        }
    }
}