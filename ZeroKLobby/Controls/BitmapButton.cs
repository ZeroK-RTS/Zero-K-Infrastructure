using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using ZeroKLobby.Controls;

namespace ZeroKLobby
{
    public class BitmapButton: Button
    {

        public BitmapButton() {
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            BackgroundImage = null;
            FlatStyle = FlatStyle.Flat;
            BackgroundImageLayout = ImageLayout.None;
            DoubleBuffered = true;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            ButtonStyle = ButtonRenderer.StyleType.DarkHive;
            SoundType = SoundPalette.SoundType.Click;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Color ForeColor { get; set; }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Color BackColor { get; set; }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Image BackgroundImage { get; set; }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        protected override bool DoubleBuffered { get; set; }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]                    
        public override Cursor Cursor { get; set; }

        public ButtonRenderer.StyleType ButtonStyle { get; set; }

        public SoundPalette.SoundType SoundType { get; set; }

        bool mouseOver;

        protected override void OnMouseEnter(EventArgs e)
        {
            mouseOver = true;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            mouseOver = false;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            SoundPalette.Play(SoundType);
            base.OnMouseClick(e);
        }

      
        protected override void OnPaint(PaintEventArgs pevent)
        {
            BackgroundImage =  ButtonRenderer.Instance.GetImageWithCache(DisplayRectangle, ButtonStyle, mouseOver ? ButtonRenderer.StyleType.DarkHiveHover : (ButtonRenderer.StyleType?)null);
            base.OnPaint(pevent);
        }
    }
}