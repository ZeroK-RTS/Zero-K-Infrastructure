using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using MissionEditor2.ContentServiceReference;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for ErrorDialog.xaml
	/// </summary>
	public partial class ErrorDialog: Window
	{
		readonly string errorLogFile;
		readonly string errorText;
		readonly Exception exception;
		readonly string version;

		public ErrorDialog(Exception exception, string errorLogFile, string version, string errorText)
		{
			this.exception = exception;
			this.errorLogFile = errorLogFile;
			this.version = version;
			this.errorText = errorText;
			InitializeComponent();
		}

		void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sendReportBox.IsChecked == true)
			{
				closeButton.IsEnabled = false;
				var service = new ContentServiceSoapClient();
				service.SubmitStackTraceCompleted += service_SubmitStackTraceCompleted;
				service.SubmitStackTraceAsync(ProgramType.MissionEditor, nameBox.Text, errorText, descriptionBox.Text, version);
				progressBar.Visibility = Visibility.Visible;
			}
			else Close();
		}

		void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			Utils.InvokeInNewThread(delegate
				{
					try
					{
						Process.Start(new ProcessStartInfo(errorLogFile));
					}
					catch {}
				});
		}

		void service_SubmitStackTraceCompleted(object sender, AsyncCompletedEventArgs e)
		{
			Close();
		}
	}
}