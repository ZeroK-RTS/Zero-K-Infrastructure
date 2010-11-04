using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App: Application
	{
		public App()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}

		void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			throw e.Exception;
		}


		void ReportError(Exception ex)
		{
			if (!Debugger.IsAttached)
			{
				if (ex == null) return;
				var version = ApplicationDeployment.IsNetworkDeployed ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString() : "Unknown";

				var text = Environment.NewLine;
				text += String.Format("Version: {0}", version) + Environment.NewLine;
				text += String.Format("Date: {0}", DateTime.Now) + Environment.NewLine;
				text += ex.GetType().Name + Environment.NewLine;
				text += ex.Message + Environment.NewLine;
				text += ex.StackTrace + Environment.NewLine;

				var errorLogFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\My Games\\Spring\\MissionEditorErrors.txt";
				File.AppendAllText(errorLogFile, text);

				Dispatcher.Invoke(new ThreadStart(delegate
					{
						var errorDialog = new ErrorDialog(ex, errorLogFile, version, text);
						errorDialog.Owner = MissionEditor2.MainWindow.Instance;
						errorDialog.ShowDialog();
						Environment.Exit(Marshal.GetHRForException(ex));
					}));
			}
		}

		void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			ReportError(e.ExceptionObject as Exception);
		}
	}
}