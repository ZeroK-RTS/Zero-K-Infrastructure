using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using mshtml;

namespace ZeroKLobby.MicroLobby
{
    public class ScrollMessageFilter: IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x20A;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_XBUTTONDOWN = 0x020B;

        public ScrollMessageFilter() {}


        private bool IsAboveBrowser(ref Message m) {
            if (Mouse.DirectlyOver == null && Program.MainWindow.navigationControl.IsBrowserTabSelected) {
                var webBrowser = Program.MainWindow.navigationControl.Browser;

                var x = ((int)m.LParam << 16) >> 16;
                var y = (int)m.LParam >> 16;

                var tl = webBrowser.PointToScreen(new Point(0, 0));
                var br = webBrowser.PointToScreen(new Point(webBrowser.ActualWidth, webBrowser.ActualHeight));
                if (new Rect(tl, br).Contains(new Point(x, y))) return true;
            }
            return false;
        }

        [DllImport("User32.dll")]
        private static extern Int32 SendMessage(int hWnd, int Msg, int wParam, int lParam);


        public bool PreFilterMessage(ref Message m) {
            if (m.Msg == WM_MOUSEWHEEL) {
                var delta = ((int)m.WParam >> 16);

                var control = MainWindow.Instance.GetHoveredControl();
                if (control == null && IsAboveBrowser(ref m)) {
                    var htmlDoc = Program.MainWindow.navigationControl.Browser.Document as HTMLDocument;
                    if (htmlDoc != null) htmlDoc.parentWindow.scrollBy(0, -delta);
                    return false;
                }
                else if (control != null) {
                    SendMessage((int)control.Handle, m.Msg, (int)m.WParam, (int)m.LParam);
                    return true;
                }
                else {
                    var elem = Mouse.DirectlyOver as FrameworkElement;
                    while (elem != null) {
                        var scr = elem as ScrollViewer;
                        if (scr != null) {
                            scr.ScrollToVerticalOffset(scr.VerticalOffset - delta);
                            return true;
                        }

                        var scroll = elem as IScrollInfo;
                        if (scroll != null) {
                            scroll.SetVerticalOffset(scroll.VerticalOffset - delta);
                            return true;
                        }

                        elem = VisualTreeHelper.GetParent(elem) as FrameworkElement;
                    }
                }
            }
            else if (m.Msg == WM_XBUTTONDOWN) {
                if (((int)m.WParam & 131072) > 0) Program.MainWindow.navigationControl.NavigateForward();
                if (((int)m.WParam & 65536) > 0) Program.MainWindow.navigationControl.NavigateBack();
            }
            return false;
        }
    }
}