using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace ZeroKLobby
{
    //]  Anarchid gradient is vertical 002c3d to 00688d with some transparency which seems fixed 
//[17:32]  Anarchid ok opacity is 65% static 

    public class BitmapButton: Button
    {

        public BitmapButton() {
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            BackgroundImage = null;
            FlatStyle = FlatStyle.Flat;
            BackgroundImageLayout = ImageLayout.None;
            ForeColor = Color.Transparent;
            BackColor = Color.Transparent;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            DoubleBuffered = true;
            ButtonStyle = ButtonStyle.DarkHiveStyle;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }


        [Description("Width of the rectangle")]
        public ButtonStyle ButtonStyle { get; set; }
      




        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            if (this.ButtonStyle != null) {
                var rend = new ButtonRenderer();
                rend.RenderToGraphics(pevent.Graphics, DisplayRectangle, ButtonStyle);
            }
        }
    }
}