using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Media;
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


        public ButtonStyle ButtonStyle { get; set; }

        bool mouseOver;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            mouseOver = true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            mouseOver = false;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            //var sp = new SoundPlayer(Sounds.button_click);
            //sp.Play();
            base.OnMouseClick(e);
        }

        protected override void OnClick(EventArgs e)
        {
            
            base.OnClick(e);
        }


        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            if (this.ButtonStyle != null) {
                var rend = new ButtonRenderer();
                rend.RenderToGraphics(pevent.Graphics, DisplayRectangle, ButtonStyle.DarkHiveStyle);
                if (mouseOver) rend.RenderToGraphics(pevent.Graphics, DisplayRectangle, ButtonStyle.DarkHiveHoverStyle);
            }
        }
    }
}