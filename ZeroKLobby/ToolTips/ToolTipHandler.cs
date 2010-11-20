using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ZeroKLobby.MicroLobby;
using Application = System.Windows.Forms.Application;
using Point = System.Drawing.Point;

namespace ZeroKLobby.ToolTips
{
    class ToolTipHandler: IMessageFilter, IDisposable
    {
        const int WM_MOUSEMOVE = 0x200;
        bool isWindowActive = true;
        bool lastActive = true;
        Point lastMousePos;
        string lastText;
        bool lastVisible = true;
        Timer timer = new Timer();


        ToolTipForm tooltip;
        readonly Dictionary<object, string> tooltips = new Dictionary<object, string>();

        bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                RefreshToolTip(false, true);
            }
        }

        public ToolTipHandler()
        {
            timer.Interval = 250;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        public void Dispose()
        {
            Application.RemoveMessageFilter(this);
            timer.Stop();
            timer = null;
        }

        public void Clear(object control)
        {
            tooltips.Remove(control);
        }


        public static string GetBattleToolTipString(int id)
        {
            return string.Format("#battle#{0}", id);
        }

        public static string GetMapToolTipString(string name)
        {
            return string.Format("#map#{0}", name);
        }


        public static string GetUserToolTipString(string name)
        {
            return string.Format("#user#{0}", name);
        }


        public void SetBattle(object control, int id)
        {
            UpdateTooltip(control, GetBattleToolTipString(id));
        }

        public void SetMap(object control, string name)
        {
            UpdateTooltip(control, GetMapToolTipString(name));
        }


        public void SetText(object target, string text)
        {
            UpdateTooltip(target, text);
        }

        public void SetUser(object control, string name)
        {
            UpdateTooltip(control, GetUserToolTipString(name));
        }

    	void RefreshToolTip(bool invalidate, bool doActiveWindowCheck)
        {
            if (Program.MainWindow != null && Program.MainWindow.IsLoaded && !Program.CloseOnNext && Program.MainWindow.IsVisible &&
                Program.MainWindow.WindowState != WindowState.Minimized)
            {
             var control = MainWindow.Instance.GetHoveredControl();
             string text = null;
             if (control != null) tooltips.TryGetValue(control, out text);
             else
             {
               var wpfElement = Mouse.DirectlyOver as FrameworkElement;
               while (wpfElement != null)
               {
                 if (wpfElement.ToolTip is string)
                 {
                   text = (string)wpfElement.ToolTip;
                   break;
                 }
                 if (wpfElement.ToolTip is ITooltipProvider)
                 {
                   text = ((ITooltipProvider)wpfElement.ToolTip).Tooltip;
                   break;
                 }
                 if (!tooltips.TryGetValue(wpfElement, out text)) {
                   var tag = wpfElement.Tag as ITooltipProvider;
                   if (tag != null) {
                     text = tag.Tooltip;
                     break;
                   }
                 } else break;
                 wpfElement = wpfElement.Parent as FrameworkElement;
               }
             }



              if (lastText != text || lastVisible != Visible || lastActive != isWindowActive)
                {
                    if (tooltip != null)
                    {
                        tooltip.Close();
                        tooltip.Dispose();
                    }

										if (doActiveWindowCheck) isWindowActive = WindowsApi.GetForegroundWindow() == (int)MainWindow.Instance.Handle;

                    if (!string.IsNullOrEmpty(text) && Visible && isWindowActive)
                    {
                        tooltip = ToolTipForm.CreateToolTipForm(text);
                        if (tooltip != null) tooltip.Visible = true;
                    }

                    lastText = text;
                    lastVisible = Visible;
                    lastActive = isWindowActive;
                }
                if (tooltip != null)
                {
                    var mp = Control.MousePosition;
                	
                		var point = MainWindow.Instance.PointToScreen(new System.Windows.Point(5,5));
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

                    tooltip.SetDesktopLocation(nx, ny);

                    var newSize = tooltip.GetTooltipSize();
                    if (newSize.HasValue && newSize.Value != tooltip.Size) tooltip.Size = newSize.Value;

                    if (invalidate) tooltip.Invalidate(true);
                }
            }
        }

        void UpdateTooltip(object control, string s)
        {
            tooltips[control] = s;
            RefreshToolTip(false, true);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_MOUSEMOVE)
            {
                var mp = Control.MousePosition;
                /*int xp = (int)m.LParam & 0xFFFF;
                int yp = (int)m.LParam >> 16;*/
                if (mp != lastMousePos) RefreshToolTip(false, false);
                lastMousePos = mp;
            }

            return false;
        }


        void timer_Tick(object sender, EventArgs e)
        {
            RefreshToolTip(true, true);
        }
    }

  interface ITooltipProvider
  {
    string Tooltip { get; }
  }
}