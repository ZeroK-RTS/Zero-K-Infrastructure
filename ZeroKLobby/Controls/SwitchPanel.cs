using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    public partial class SwitchPanel : Panel
    {
        public SwitchPanel()
        {
            InitializeComponent();
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Color BackColor { get { return base.BackColor; } set { base.BackColor = value; } }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        protected override bool DoubleBuffered { get { return base.DoubleBuffered; } set { base.DoubleBuffered = value; } }

        Control currentTarget;

        public enum AnimType
        {
            SlideLeft = 1,
            SlideBottom  =2
        }

        private void SlideLeft(Control c, Rectangle r, double percent)
        {
            c.Left = (int)Math.Round(-r.Width * percent); 
        }

        private void SlideBottom(Control c, Rectangle r, double percent)
        {
            c.Top = (int)Math.Round(r.Height * percent); 
        }


        public async Task SwitchContent(Control newTarget, AnimType? animation = null)
        {

            var animator = GetAnimator(animation);

            if (currentTarget != null && animator != null) {
                var stepCount = 10;
                var stepDelay = 10;
                SoundPalette.Play(SoundPalette.SoundType.Servo);

                Rectangle r = GetChildrenBoundingRectangle(currentTarget);

                for (int i = 0; i < stepCount; i++) {
                    foreach (var c in currentTarget.Controls.Cast<Control>()) {
                        animator(c, r, (double)i/stepCount);
                    }
                    await Task.Delay(stepDelay);
                }
                this.Controls.Remove(currentTarget);
                this.Controls.Clear();

                newTarget.Dock = DockStyle.Fill;
                Controls.Add(newTarget);

                r = GetChildrenBoundingRectangle(newTarget);

                for (int i = stepCount; i >=0; i--)
                {
                    foreach (var c in newTarget.Controls.Cast<Control>()) {
                        animator(c, r, (double)i/stepCount);
                    }
                    await Task.Delay(stepDelay);
                }

                currentTarget = newTarget;
            } else {
                newTarget.Dock = DockStyle.Fill;
                this.Controls.Add(newTarget);
                currentTarget = newTarget;
            }
        }

        static Rectangle GetChildrenBoundingRectangle(Control currentTarget)
        {
            var r = Rectangle.Empty;
            foreach (var c in currentTarget.Controls.OfType<Control>()) {
                if (r.Width < c.Right) r.Width = c.Right;
                if (r.Height < c.Bottom) r.Height = c.Bottom;
            }
            return r;
        }

        Action<Control, Rectangle, double> GetAnimator(AnimType? animation)
        {
            Action<Control, Rectangle, double> animator = null;
            if (animation == AnimType.SlideLeft) animator = SlideLeft;
            else if (animation == AnimType.SlideBottom) animator = SlideBottom;
            return animator;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        private void PaintParentBackground(PaintEventArgs e)
        {
            if (Parent != null)
            {
                Rectangle rect = new Rectangle(Left, Top,
                                               Width, Height);

                e.Graphics.TranslateTransform(-rect.X, -rect.Y);

                try
                {
                    using (PaintEventArgs pea =
                                new PaintEventArgs(e.Graphics, rect))
                    {
                        pea.Graphics.SetClip(rect);
                        InvokePaintBackground(Parent, pea);
                        InvokePaint(Parent, pea);
                    }
                }
                finally
                {
                    e.Graphics.TranslateTransform(rect.X, rect.Y);
                }
            }
            else
            {
                e.Graphics.FillRectangle(SystemBrushes.Control,
                                         ClientRectangle);
            }
        }
    }
}
