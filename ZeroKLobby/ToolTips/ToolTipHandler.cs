using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Forms.Application;
using Control = System.Windows.Controls.Control;
using Point = System.Drawing.Point;

namespace ZeroKLobby
{
    public class ToolTipHandler: IMessageFilter, IDisposable
    {
        public static readonly DependencyProperty MyToolTipProperty = DependencyProperty.RegisterAttached("MyToolTip", typeof(string), typeof(Control), new UIPropertyMetadata(null));


        private const int WM_MOUSEMOVE = 0x200;
        private bool lastActive = true;
        private Point lastMousePos;
        private string lastText;
        private bool lastVisible = true;
        private Timer timer = new Timer();


        private ToolTipForm tooltip;
        private readonly Dictionary<object, string> tooltips = new Dictionary<object, string>();

        private bool visible = true;
        public bool Visible {
            get { return visible; }
            set {
                visible = value;
                RefreshToolTip(false);
            }
        }

        public ToolTipHandler() {
            timer.Interval = 250;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        public void Dispose() {
            Application.RemoveMessageFilter(this);
            timer.Stop();
            timer = null;
        }

        public void Clear(object control) {
            tooltips.Remove(control);
        }


        public static string GetBattleToolTipString(int id) {
            return string.Format("#battle#{0}", id);
        }

        public static string GetMapToolTipString(string name) {
            return string.Format("#map#{0}", name);
        }

        public static string GetMyToolTip(DependencyObject obj) {
            return (string)obj.GetValue(MyToolTipProperty);
        }


        public static string GetUserToolTipString(string name) {
            return string.Format("#user#{0}", name);
        }


        public void SetBattle(object control, int id) {
            UpdateTooltip(control, GetBattleToolTipString(id));
        }

        public void SetMap(object control, string name) {
            UpdateTooltip(control, GetMapToolTipString(name));
        }

        public static void SetMyToolTip(DependencyObject obj, string value) {
            obj.SetValue(MyToolTipProperty, value);
        }


        public void SetText(object target, string text) {
            UpdateTooltip(target, text);
        }

        public void SetUser(object control, string name) {
            UpdateTooltip(control, GetUserToolTipString(name));
        }

        private void RefreshToolTip(bool invalidate) {
            if (Program.MainWindow != null && Program.MainWindow.IsLoaded && !Program.CloseOnNext && Program.MainWindow.IsVisible && Program.MainWindow.WindowState != WindowState.Minimized) {

                var control = MainWindow.Instance.GetHoveredControl();
                string text = null;
                if (control != null) tooltips.TryGetValue(control, out text);
                else {
                    var wpfElement = Mouse.DirectlyOver as FrameworkElement;
                    while (wpfElement != null) {
                        if (!string.IsNullOrEmpty(wpfElement.GetValue(MyToolTipProperty) as string)) {
                            text = wpfElement.GetValue(MyToolTipProperty) as string;
                            break;
                        }

                        if (!tooltips.TryGetValue(wpfElement, out text)) {
                            var tag = wpfElement.Tag as IToolTipProvider;
                            if (tag != null) {
                                text = tag.ToolTip;
                                break;
                            }
                        }
                        else break;

                        wpfElement = VisualTreeHelper.GetParent(wpfElement) as FrameworkElement;
                    }
                }

                bool isWindowActive = WindowsApi.GetForegroundWindow() == (int)MainWindow.Instance.Handle || WindowsApi.GetForegroundWindow() == WindowsApi.GetActiveWindow();

                if (lastText != text || lastVisible != Visible || lastActive != isWindowActive) {
                    if (tooltip != null) {
                        tooltip.Close();
                        tooltip.Dispose();
                        tooltip = null;
                    }

                    if (!string.IsNullOrEmpty(text) && Visible && isWindowActive) {
                        tooltip = ToolTipForm.CreateToolTipForm(text);
                        if (tooltip != null) tooltip.Visible = true;
                    }

                    lastText = text;
                    lastVisible = visible;
                    lastActive = isWindowActive;
                }

                if (tooltip != null) {
                    var mp = System.Windows.Forms.Control.MousePosition;

                    var point = MainWindow.Instance.PointToScreen(new System.Windows.Point(5, 5));
                    var scr = Screen.GetWorkingArea(new Point((int)point.X, (int)point.Y));

                    //need screen0's bounds because SetDesktopLocation is relative to screen0.
                    var scr1 = Screen.AllScreens[0].WorkingArea;
                    var scr1B = Screen.AllScreens[0].Bounds;

                    var nx = Math.Min(mp.X + 14 + scr1B.X - scr1.X, scr.Right - tooltip.Width - 2);
                    var ny = Math.Min(mp.Y + 14 + scr1B.Y - scr1.Y, scr.Bottom - tooltip.Height - 2);

                    var rect = new Rectangle(nx, ny, tooltip.Width, tooltip.Height);
                    if (rect.Contains(mp)) {
                        nx = mp.X - tooltip.Width - 8;
                        ny = mp.Y - tooltip.Height - 8;
                    }

                    tooltip.SetDesktopLocation(nx, ny);

                    var newSize = tooltip.GetTooltipSize();
                    if (newSize.HasValue && newSize.Value != tooltip.Size) tooltip.Size = newSize.Value;

                    if (invalidate) tooltip.Invalidate(true);
                }
            }
        }

        private void UpdateTooltip(object control, string s) {
            tooltips[control] = s;
            RefreshToolTip(false);
        }

        public bool PreFilterMessage(ref Message m) {
            if (m.Msg == WM_MOUSEMOVE) {
                var mp = System.Windows.Forms.Control.MousePosition;
                /*int xp = (int)m.LParam & 0xFFFF;
                int yp = (int)m.LParam >> 16;*/
                if (mp != lastMousePos) RefreshToolTip(false);
                lastMousePos = mp;
            }

            return false;
        }


        private void timer_Tick(object sender, EventArgs e) {
            RefreshToolTip(true);
        }
    }

    internal interface IToolTipProvider
    {
        string ToolTip { get; }
    }
}