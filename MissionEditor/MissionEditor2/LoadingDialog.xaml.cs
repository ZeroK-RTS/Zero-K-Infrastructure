using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for LoadingDialog.xaml
	/// </summary>
	public partial class LoadingDialog : Window
	{
		public LoadingDialog()
		{
			InitializeComponent();
		}

		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;
		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		public string Text
		{
			get { return textBlock.Text; }
			set
			{
				Dispatcher.BeginInvoke(new ThreadStart(() => textBlock.Text = value));
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var hwnd = new WindowInteropHelper(this).Handle;
			SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU); // hide the close button
		}
	}
}