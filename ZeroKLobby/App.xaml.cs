using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
			this.MainWindow = new MainWindow(); // this is  a hack to make wpf app not close after you open/close some other wpf window - wont be needed in full wpf app
			this.MainWindow.Visibility = Visibility.Hidden;
			Program.Initialize(e.Args);
			
			//this.Shutdown();
		}

		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			// intentionally left empty, just subscribing to this event prevents WPF from swallowing its exceptions
		}
	}
}
