using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ZeroKLobby
{
    public class ToolTipHandler: IMessageFilter, IDisposable
    {
        private const int WM_MOUSEMOVE = 0x200;
        const int WM_KEYDOWN = 0x0100;
        const Int32 HWND_TOPMOST = -1;
        const Int32 SWP_NOACTIVATE = 0x0010;
        const Int32 SWP_NOSIZE = 0x0001;
        const Int32 SWP_ASYNCWINDOWPOS = 0x4000;
        const Int32 SWP_NOCOPYBITS = 0x0100;
        const Int32 SWP_DEFERERASE = 0x2000;
        const Int32 SWP_NOMOVE = 0x0002;
        const Int32 SWP_NOREDRAW = 0x0008;
        const Int32 SWP_NOSENDCHANGING = 0x0400;
        const Int32 SWP_ToolTipOption = SWP_NOACTIVATE | SWP_NOSIZE | SWP_ASYNCWINDOWPOS | SWP_NOCOPYBITS 
            | SWP_DEFERERASE | SWP_NOMOVE | SWP_NOREDRAW | SWP_NOSENDCHANGING; //disable all SetWindowPos() function except Z-ordering
        private bool lastActive = true;
        private Point lastMousePos;
        private string lastText;
        private bool lastVisible = true;
        private Timer uiTimer;
        const int timerFPS = 70;

        public DateTime LastUserAction = DateTime.Now;


        private ToolTipForm tooltip;
        private readonly Dictionary<object, string> tooltips = new Dictionary<object, string>();

        private bool visible = true;
        public bool Visible {
            get { return visible; }
            set {
                visible = value;
                requestRefresh = true;// RefreshToolTip(true);
            }
        }

        public ToolTipHandler() {
            uiTimer = new Timer();
            uiTimer.Interval = 1000 / timerFPS;
            uiTimer.Tick += uiTimer_Tick;
            uiTimer.Start();
        }

        public void Dispose() {
            Application.RemoveMessageFilter(this);
            if (uiTimer != null)
            {
                uiTimer.Stop();
                uiTimer = null;
            }
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

        public static string GetUserToolTipString(string name) {
            return string.Format("#user#{0}", name);
        }


        public void SetBattle(object control, int id) {
            UpdateTooltip(control, GetBattleToolTipString(id));
        }

        public void SetMap(object control, string name) {
            UpdateTooltip(control, GetMapToolTipString(name));
        }

        public void SetText(object target, string text) {
            UpdateTooltip(target, text);
        }

        public void SetUser(object control, string name) {
            UpdateTooltip(control, GetUserToolTipString(name));
        }

        //Copied from PopupNotify.cs
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, Int32 hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, uint uFlags);

        private void RefreshToolTip(bool invalidate) {
            if (Program.MainWindow != null && Program.MainWindow.IsHandleCreated && !Program.CloseOnNext && Program.MainWindow.Visible && Program.MainWindow.WindowState != FormWindowState.Minimized) {

                var control = MainWindow.Instance.GetHoveredControl();
                string text = null;
                if (control != null) tooltips.TryGetValue(control, out text);
                bool isWindowActive = Form.ActiveForm != null;
                //bool newTooltip = false; //to trigger position update for Method A

                if (lastText != text || lastVisible != Visible || lastActive != isWindowActive) {
                    if (tooltip != null)
                        tooltip.IsActive = false;

                    if (!string.IsNullOrEmpty(text) && Visible && isWindowActive) {
                        tooltip = ToolTipForm.CreateToolTipForm(text);
                        if (tooltip.IsActive)
                        {
                            tooltip.ForeColor = Program.Conf.OtherTextColor;
                            tooltip.Visible = true;
                        }
                    }

                    lastText = text;
                    lastVisible = visible;
                    lastActive = isWindowActive;
                    //newTooltip = true; //trigger position update
                }

                if (tooltip != null && tooltip.IsActive) {
                    //method B: tooltip remain stationary until user block the vision or when new tooltip is available
                    //var mp = System.Windows.Forms.Control.MousePosition;
                    //int tooltipLocationX = tooltip.Location.X;
                    //int tooltipLocationY = tooltip.Location.Y;
                    //if (mp.X > tooltipLocationX && mp.X < tooltipLocationX + tooltip.Width)
                    //{
                    //    if (mp.Y > tooltipLocationY && mp.Y < tooltipLocationY + tooltip.Height)
                    //    {
                    //        newTooltip = true;
                    //    }
                    //}
                    //if (Math.Abs(mp.X - tooltipLocationX) > 50 || Math.Abs(mp.Y - tooltipLocationY) > 50)
                    //{
                    //    newTooltip = true;
                    //}
                    //if (newTooltip) //set new position for new tooltip
                    //{
                    //    var point = MainWindow.Instance.PointToScreen(new Point(5, 5));
                    //    var scr = Screen.GetWorkingArea(new Point((int)point.X, (int)point.Y));

                    //    //need screen0's bounds because SetDesktopLocation is relative to screen0.
                    //    var scr1 = Screen.AllScreens[0].WorkingArea;
                    //    var scr1B = Screen.AllScreens[0].Bounds;

                    //    var nx = Math.Min(mp.X + 14 + scr1B.X - scr1.X, scr.Right - tooltip.Width - 2);
                    //    var ny = Math.Min(mp.Y + 14 + scr1B.Y - scr1.Y, scr.Bottom - tooltip.Height - 2);

                    //    var rect = new Rectangle(nx, ny, tooltip.Width, tooltip.Height);
                    //    if (rect.Contains(mp))
                    //    {
                    //        nx = mp.X - tooltip.Width - 8;
                    //        ny = mp.Y - tooltip.Height - 8;
                    //    }

                    //    SetWindowPos(tooltip.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_ToolTipOption);
                    //    tooltip.SetDesktopLocation(nx, ny);

                    //    var newSize = tooltip.GetTooltipSize();
                    //    if (newSize.HasValue && newSize.Value != tooltip.Size) tooltip.Size = newSize.Value;
                    //} 
                    //end method B
                    
                    //method A: tooltip follow mouse cursor everywhere it go
                    var mp = System.Windows.Forms.Control.MousePosition;

                    var point = MainWindow.Instance.PointToScreen(new Point(5, 5));
                    var scr = Screen.GetWorkingArea(new Point((int)point.X, (int)point.Y));

                    //need screen0's bounds because SetDesktopLocation is relative to screen0.
                    var scr1 = Screen.AllScreens[0].WorkingArea;
                    var scr1B = Screen.AllScreens[0].Bounds;

                    var nx = Math.Min(mp.X + 14 + scr1B.X - scr1.X, scr.Right - tooltip.Width - 2);
                    var ny = Math.Min(mp.Y + 14 + scr1B.Y - scr1.Y, scr.Bottom - tooltip.Height - 2);

                    var rect = new Rectangle(nx, ny, tooltip.Width, tooltip.Height);
                    if (rect.Contains(mp))
                    {
                        nx = mp.X - tooltip.Width - 8;
                        ny = mp.Y - tooltip.Height - 8;
                    }

                    SetWindowPos(tooltip.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_ToolTipOption); //refresh tooltip's Z-order to be on top
                    tooltip.SetDesktopLocation(nx, ny); //note: SetWindowPos() positioning won't work in Linux (so we shouldn't replace this with SetWindowPos)

                    var newSize = tooltip.GetTooltipSize();
                    if (newSize.HasValue && newSize.Value != tooltip.Size) tooltip.Size = newSize.Value;
                    //end method A

                    if (invalidate) tooltip.Invalidate(true);
                }
            }
        }

        private bool requestRefresh = false;
        private void UpdateTooltip(object control, string s) {
            (control as Control).Disposed += (sender, e) => 
            {
                Clear(sender); 
            };
            tooltips[control] = s;
            requestRefresh = true; //RefreshToolTip(true);
        }

        private bool mouseMoving = false;
        public bool PreFilterMessage(ref Message m) {
            if (m.Msg == WM_KEYDOWN || m.Msg == WM_MOUSEMOVE) LastUserAction = DateTime.Now;
            
            if (m.Msg == WM_MOUSEMOVE)
            { // && Environment.OSVersion.Platform != PlatformID.Unix 
                var mp = Control.MousePosition;
                //int xp = (int)m.LParam & 0xFFFF;
                //int yp = (int)m.LParam >> 16;

                if (mp != lastMousePos) mouseMoving = true;
                else mouseMoving = false;
                lastMousePos = mp;
            }

            return false;
        }

        int frameCount = 0;
        private void uiTimer_Tick(object sender, EventArgs e)
        {
            if (!Visible) return;

            frameCount++;
            bool oneSec = (frameCount >= timerFPS);
            if (mouseMoving || requestRefresh || oneSec)
            {
                RefreshToolTip(requestRefresh || oneSec);
                requestRefresh = false;
                if (oneSec) frameCount = 0;
            }
        }
    }

    internal interface IToolTipProvider
    {
        string ToolTip { get; }
    }
}