using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
#if (!LINUX)
using mshtml; 
//will not compile in MonoDevelop
//using preprocessor directive to skip. reference: http://msdn.microsoft.com/en-us/library/yt3yck0x.aspx
//"LINUX" symbol is defined in LinuxDebug Project-option
#endif


namespace ZeroKLobby.MicroLobby
{
    public class ScrollMessageFilter: IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x20A;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_XBUTTONDOWN = 0x020B;

        
        [DllImport("User32.dll")]
        private static extern Int32 SendMessage(int hWnd, int Msg, int wParam, int lParam);


        public bool PreFilterMessage(ref Message m) {
            if (m.Msg == WM_MOUSEWHEEL) {
                var control = MainWindow.Instance.GetHoveredControl();
                if (control != null) {

                    if (control is WebBrowser) {
						#if (!LINUX)
						var brows = control as WebBrowser;
                        if (brows.Document != null) //check whether the page exist before adding a scroll bar
                        {

						    var delta = ((int)m.WParam >> 16);
                            var htmlDoc = brows.Document.DomDocument as HTMLDocument;
                            if (htmlDoc != null) htmlDoc.parentWindow.scrollBy(0, -delta);
						}
						#endif
                    }
                    else {
                        if (Environment.OSVersion.Platform == PlatformID.Unix) {
                            if (!control.Focused) control.Focus(); // this is needed on linux for some stupid reason
                            var winforms = Assembly.GetAssembly(typeof(System.Windows.Forms.Control));
                            var xplat = winforms.GetType("System.Windows.Forms.XplatUI");
                            if (xplat != null) {
                                var mi = xplat.GetMethod("SendMessage",
                                                         BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                                                         null,
                                                         new[] { winforms.GetType("System.Windows.Forms.Message&") },
                                                         null);
                                if (mi != null) mi.Invoke(null, new object[] { m });
                                else Trace.TraceError("Method SendMessage not found in XplatUI");
                            }
                            else Trace.TraceError("XplatUI not found");
                        }
                        else SendMessage((int)control.Handle, m.Msg, (int)m.WParam, (int)m.LParam);
                    }

                    return true;
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