using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using ZkData;

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

      
        const int animationTimeMs = 150;
        const int stepDelay = 10;

        public TabPage SetupTabPage(Control content)
        {
            var tab = Controls.OfType<TransparentTabPage>().FirstOrDefault(x => x.Controls.OfType<Control>().First() == content);
            if (tab == null) {
                tab = new TransparentTabPage();
                content.Dock = DockStyle.Fill;
                tab.Controls.Add(content);
                TabPages.Add(tab);
            }
            return tab;
        }


        public async Task SwitchContent(Control newTarget, AnimType? animation = null)
        {
            var animator = GetAnimator(animation);

            if (CurrentTarget != null && animator != null) {

                var sw = new Stopwatch();
                Rectangle r = GetChildrenBoundingRectangle(CurrentTarget);

                var bounds = CurrentTarget.Controls.OfType<Control>().ToDictionary(x => x, x => x.Bounds);

                sw.Start();
                long elapsed;
                while ((elapsed = sw.ElapsedMilliseconds) < animationTimeMs) {
                    foreach (var c in CurrentTarget.Controls.Cast<Control>()) {
                        animator(c, r, (double)elapsed/animationTimeMs, bounds[c]);
                    }
                    await Task.Delay(stepDelay);
                } 
                sw.Stop();

                foreach (var b in bounds) { b.Key.Bounds = b.Value; }

                var tab = SetupTabPage(newTarget);
                SelectTab(tab);
                CurrentTarget = newTarget;
                
                r = GetChildrenBoundingRectangle(newTarget);

                bounds = newTarget.Controls.OfType<Control>().ToDictionary(x => x, x => x.Bounds);

                sw.Start();
                while ((elapsed = sw.ElapsedMilliseconds) < animationTimeMs)
                {
                    foreach (var c in CurrentTarget.Controls.Cast<Control>())
                    {
                        animator(c, r, 1.0-(double)elapsed / animationTimeMs, bounds[c]);
                    }
                    await Task.Delay(stepDelay);
                } 
                sw.Stop();

                foreach (var b in bounds) { b.Key.Bounds = b.Value; }

                CurrentTarget = newTarget;
            } else {
                SelectTab(SetupTabPage(newTarget));
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
