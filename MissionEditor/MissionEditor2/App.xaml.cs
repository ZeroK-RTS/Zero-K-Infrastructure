using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
#if !DEBUG
			var ex = e.Exception;
			var exceptionName = ex.GetType().Name;
			var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


			var text = exceptionName + "\r\n" + ex.Message + "\r\n" + ex.StackTrace;
			File.AppendAllText(assemblyLocation + "\\errors.log", text + "\n");

			var message = String.Format("{0} error.\r\n{1}\r\nSee {2}\\errors.log for details.\r\n", exceptionName, ex.Message,
									assemblyLocation);
			MessageBox.Show(message);

			Environment.Exit(1);
#endif
		}
	}
}
