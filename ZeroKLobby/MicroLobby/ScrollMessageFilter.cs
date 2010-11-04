using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using mshtml;

namespace ZeroKLobby.MicroLobby
{
	public class ScrollMessageFilter: IMessageFilter
	{
		const int WM_MOUSEWHEEL = 0x20A;
		const int WM_RBUTTONDOWN = 0x204;
		const int WM_XBUTTONDOWN = 0x020B;

		public ScrollMessageFilter() {}


		bool IsAboveBrowser(ref Message m)
		{
			if (Mouse.DirectlyOver == null && Program.MainWindow.navigationControl.IsBrowserTabSelected)
			{
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
		static extern Int32 SendMessage(int hWnd, int Msg, int wParam, int lParam);


		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg == WM_MOUSEWHEEL)
			{
				var control = MainWindow.Instance.GetHoveredControl();
				if (control == null && IsAboveBrowser(ref m))
				{
					var htmlDoc = Program.MainWindow.navigationControl.Browser.Document as HTMLDocument;
					var delta = ((int)m.WParam >> 16);
					if (htmlDoc != null) htmlDoc.parentWindow.scrollBy(0, -delta);
					return false;
				}
				else if (control != null)
				{
					SendMessage((int)control.Handle, m.Msg, (int)m.WParam, (int)m.LParam);
					return true;
				}
			}
			else if (m.Msg == WM_XBUTTONDOWN) Program.MainWindow.navigationControl.NavigateBack();
			return false;
		}
	}
}