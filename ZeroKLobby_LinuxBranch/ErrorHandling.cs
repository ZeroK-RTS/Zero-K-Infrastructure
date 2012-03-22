using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace ZeroKLobby
{
	public static class ErrorHandling
	{
		public static void HandleException(Exception e, bool isCrash)
		{
			HandleException((isCrash ? "FATAL: " : "") + e, isCrash);
		}

		static void HandleException(string e, bool sendWeb)
		{
			try
			{
				// write to error log
				using (var s = File.AppendText(Utils.MakePath(Program.SpringPaths.WritableDirectory, Config.LogFile))) s.WriteLine("===============\r\n{0}\r\n{1}\r\n", DateTime.Now.ToString("g"), e);

				var version = ApplicationDeployment.IsNetworkDeployed && ApplicationDeployment.CurrentDeployment != null
												? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
												: Application.ProductVersion;
				// send to error gathering site
				if (!Debugger.IsAttached && sendWeb && Program.Conf != null)
				{
					using (var wc = new WebClient { Proxy = null })
					{
						var urtext = string.Format("{0}?username={1}&version={2}&exception={3}", Config.ReportUrl, Program.Conf.LobbyPlayerName, version, e);
						wc.DownloadString(new Uri(urtext));
					}
				}
			}
			catch {}
		}
	}
}