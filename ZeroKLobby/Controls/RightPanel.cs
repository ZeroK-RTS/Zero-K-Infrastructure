using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    public partial class RightPanel : Panel
    {
        public RightPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            BackgroundImage = null;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (BackgroundImage == null) {
                var parent = this.FindParentWithBgImage();
                if (parent != null) {

                    var img = new Bitmap(Width, Height);
                    using (var g = Graphics.FromImage(img)) {
                        g.TranslateTransform(-Left, -Top);
                        InvokePaintBackground(parent, new PaintEventArgs(g, Bounds));
                        g.TranslateTransform(Left, Top);

                        FrameBorderRenderer.Instance.RenderToGraphics(g, new Rectangle(0,0,Width,Height), FrameBorderRenderer.StyleType.Shraka);
                    }
                    BackgroundImageLayout = ImageLayout.None;
                    BackgroundImage = img;
                }
            }

            if (BackgroundImage != null) e.Graphics.DrawImage(BackgroundImage, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);

        }
    }
}
