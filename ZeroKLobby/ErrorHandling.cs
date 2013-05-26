using System;
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
			}
			catch {}
		}
	}
}