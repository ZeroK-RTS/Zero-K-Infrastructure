using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    public class ScrollMessageFilter: IMessageFilter
    {
        const int WM_MOUSEWHEEL = 0x20A;

        UIElement filterForm;

        public ScrollMessageFilter(UIElement filterForm)
        {
            this.filterForm = filterForm;
        }


        [DllImport("User32.dll")]
        static extern Int32 SendMessage(int hWnd, int Msg, int wParam, int lParam);

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL)
            {
                var control = MainWindow.Instance.GetHoveredControl();
								if (control == null) return false;
                if (control is WebBrowser) return false;
								SendMessage((int)control.Handle, m.Msg, (int)m.WParam, (int)m.LParam);
                return true;
            }
            return false;
        }
    }
}