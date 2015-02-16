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

        public Control CurrentTarget { get; private set; }

        public enum AnimType
        {
            SlideLeft = 1,
            SlideBottom  =2
        }

        private void SlideLeft(Control c, Rectangle r, double percent, Rectangle original)
        {
            c.Left = (int)Math.Round(original.Left-r.Width * percent);
        }

        private void SlideBottom(Control c, Rectangle r, double percent, Rectangle original)
        {
            c.Top = (int)Math.Round(original.Top + r.Height * percent); 
        }

        void DrawSyblings(PaintEventArgs e)
        {
            if (Parent != null) {
                float tx = -Left, ty = -Top;

                // make adjustments to tx and ty here if your control
                // has a non-client area, borders or similar

                e.Graphics.TranslateTransform(tx, ty);

                using (PaintEventArgs pea = new PaintEventArgs(e.Graphics, e.ClipRectangle)) {
                    InvokePaintBackground(Parent, pea);
                    InvokePaint(Parent, pea);
                }

                e.Graphics.TranslateTransform(-tx, -ty);

                // loop through children of parent which are under ourselves
                int start = Parent.Controls.GetChildIndex(this);
                Rectangle rect = new Rectangle(Left, Top, Width, Height);
                for (int i = Parent.Controls.Count - 1; i > start; i--) {
                    Control c = Parent.Controls[i];

                    // skip ...
                    // ... invisible controls
                    // ... or controls that have zero width/height (Autosize Labels without content!)
                    // ... or controls that don't intersect with ourselves
                    if (!c.Visible || c.Width == 0 || c.Height == 0 || !rect.IntersectsWith(new Rectangle(c.Left, c.Top, c.Width, c.Height))) continue;

                    using (Bitmap b = new Bitmap(c.Width, c.Height, e.Graphics)) {
                        c.DrawToBitmap(b, new Rectangle(0, 0, c.Width, c.Height));

                        tx = c.Left - Left;
                        ty = c.Top - Top;

                        // make adjustments to tx and ty here if your control
                        // has a non-client area, borders or similar

                        e.Graphics.TranslateTransform(tx, ty);
                        e.Graphics.DrawImageUnscaled(b, new Point(0, 0));
                        e.Graphics.TranslateTransform(-tx, -ty);
                    }
                }
            }
        }


        const int stepCount = 10;
        const int stepDelay = 10;


        public async Task SwitchContent(Control newTarget, AnimType? animation = null)
        {
            var animator = GetAnimator(animation);

            if (CurrentTarget != null && animator != null) {

                Rectangle r = GetChildrenBoundingRectangle(CurrentTarget);

                var bounds = CurrentTarget.Controls.OfType<Control>().ToDictionary(x => x, x => x.Bounds);

                for (int i = 0; i < stepCount; i++) {
                    foreach (var c in CurrentTarget.Controls.Cast<Control>()) {
                        animator(c, r, (double)i/stepCount, bounds[c]);
                    }
                    await Task.Delay(stepDelay);
                }

                foreach (var b in bounds) { b.Key.Bounds = b.Value; }
                this.Controls.Remove(CurrentTarget);
                this.Controls.Clear();
                
                newTarget.Dock = DockStyle.Fill;
                Controls.Add(newTarget);
                
                r = GetChildrenBoundingRectangle(newTarget);

                bounds = newTarget.Controls.OfType<Control>().ToDictionary(x => x, x => x.Bounds);

                for (int i = stepCount; i >=0; i--)
                {
                    foreach (var c in newTarget.Controls.Cast<Control>()) {
                        animator(c, r, (double)i/stepCount, bounds[c]);
                    }
                    await Task.Delay(stepDelay);
                }
                foreach (var b in bounds) { b.Key.Bounds = b.Value; }

                CurrentTarget = newTarget;
            } else {
                if (CurrentTarget != null) this.Controls.Remove(CurrentTarget);
                newTarget.Dock = DockStyle.Fill;
                this.Controls.Add(newTarget);
                CurrentTarget = newTarget;
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

        Action<Control, Rectangle, double, Rectangle> GetAnimator(AnimType? animation)
        {
            Action<Control, Rectangle, double, Rectangle> animator = null;
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
