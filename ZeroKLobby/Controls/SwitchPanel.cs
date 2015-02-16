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
    public class SwitchPanel:HeadlessTabControl
    {
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

      
        const int stepCount = 10;
        const int stepDelay = 10;


        public async Task SwitchContent(Control newTarget, AnimType? animation = null)
        {
            var tab = new TabPage();
            newTarget.Dock = DockStyle.Fill;
            tab.Controls.Add(newTarget);
            TabPages.Add(tab);
            SelectTab(tab);
            CurrentTarget = newTarget;
            return;
            

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


    }
}
