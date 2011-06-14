using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
    bool hasStarted = false;

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (Program.Main(e.Args))
			{
                hasStarted = true;
				MainWindow = Program.MainWindow;
				MainWindow.Show();
                if (Program.Conf.ShowFriendsWindow == true)
                {
                    FriendsWindow frWindow = new FriendsWindow();
                    frWindow.Show();
                }
			} else Shutdown();
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
      if (hasStarted) Program.ShutDown();
		}
	}
}
