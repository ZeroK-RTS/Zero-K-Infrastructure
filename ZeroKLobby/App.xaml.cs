using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace ZeroKLobby
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{


		private void Application_Startup(object sender, StartupEventArgs e)
		{
			Program.Main(null); // todo some exception handlign (dont catch in main? )
			MainWindow = Program.MainWindow;
			MainWindow.Show();
		}

		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			if (Debugger.IsAttached) throw e.Exception;
			ErrorHandling.HandleException(e.Exception, true);
			Trace.TraceError("Unhandled WPF exception: {0}", e.Exception);
			e.Handled = true;
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{

		}
	}
}
