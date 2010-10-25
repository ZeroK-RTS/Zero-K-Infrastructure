using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ZeroKLobby.Notifications;
using Color = System.Drawing.Color;
using MessageBox = System.Windows.Forms.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace ZeroKLobby
{
	static class Utils
	{
		public static bool IsDesignTime
		{
			get { return System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()); }
		}

		public static bool CanRead(string filename)
		{
			if (!File.Exists(filename)) return true;
			try
			{
				using (var f = File.Open(filename, FileMode.Open, FileAccess.Read)) {}
				return true;
			}
			catch
			{
				return false;
			}
		}


		public static bool CanWrite(string filename)
		{
			if (!File.Exists(filename)) return true;
			try
			{
				using (var f = File.Open(filename, FileMode.Open, FileAccess.Write)) {}
				return true;
			}
			catch
			{
				return false;
			}
		}


		public static void CheckPath(string path)
		{
			CheckPath(path, false);
		}

		public static void CheckPath(string path, bool delete)
		{
			if (delete)
			{
				try
				{
					Directory.Delete(path, true);
				}
				catch {}
			}
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
		}

		public static bool CheckSpringFolder(string path, out string fixedSpringPath)
		{
			fixedSpringPath = path;
			try
			{
				if (IsCorrectPath(ref fixedSpringPath)) return true;

				fixedSpringPath = Directory.GetCurrentDirectory();
				if (IsCorrectPath(ref fixedSpringPath)) return true;

				var regPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Spring", "DisplayIcon", "");
				fixedSpringPath = Path.GetDirectoryName(regPath);
				if (IsCorrectPath(ref fixedSpringPath)) return true;
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error detecting spring path: {0}", ex);
			}

			using (var od = new OpenFileDialog())
			{
				od.FileName = Config.SpringName;
				od.DefaultExt = Path.GetExtension(Config.SpringName);
				od.InitialDirectory = Program.Conf.ManualSpringPath;
				od.Title = "Please select your spring installation folder";
				od.RestoreDirectory = true;
				od.CheckFileExists = true;
				od.CheckPathExists = true;
				od.AddExtension = true;
				od.Filter = String.Format("Executable (*{0})|*{0}", od.DefaultExt);
				var dr = od.ShowDialog();
				if (dr == DialogResult.OK)
				{
					fixedSpringPath = Path.GetDirectoryName(od.FileName);
					return true;
				}
				else
				{
					if (
						MessageBox.Show("I cannot continue without valid path to spring.exe!\r\n Do you want to download Spring engine?",
						                "Spring engine not found",
						                MessageBoxButtons.YesNo,
						                MessageBoxIcon.Warning) == DialogResult.Yes) OpenWeb(NewSpringBar.DownloadUrl);
					return false;
				}
			}
		}

		public static string GetAlternativeFileName(string to)
		{
			if (File.Exists(to))
			{
				var ext = Path.GetExtension(to);
				var name = Path.GetFileNameWithoutExtension(to);
				var dir = Path.GetDirectoryName(to);
				var i = 1;
				do
				{
					to = MakePath(dir, name + "(" + i++ + ")" + ext);
				} while (File.Exists(to));
			}
			return to;
		}


		public static Control GetHoveredControl(UIElement parentControl)
		{
			/*	hack rewrite!	
			 * var screenPoint = Control.MousePosition;
						var parentPoint = parentControl.PointToClient(screenPoint);
						Control child;
						while (
							(child =
							 parentControl.GetChildAtPoint(parentPoint, GetChildAtPointSkip.Disabled | GetChildAtPointSkip.Invisible | GetChildAtPointSkip.Transparent)) !=
							null)
						{
							parentControl = child;
							parentPoint = parentControl.PointToClient(screenPoint);
						}
			 return parentControl;
			 */
			return null;
			
		}

		public static Color Invert(this Color color)
		{
			return Color.FromArgb(color.A, 255 - color.R, 255 - color.G, 255 - color.B);
		}

		public static bool IsCorrectPath(ref string full)
		{
			try
			{
				full = Path.GetFullPath(full);
				//File.Exists(Utils.MakePath(full, Program.SpringName)) && 
				if (File.Exists(MakePath(full, Config.SpringName))) return true;
			}
			catch {}
			return false;
		}

		public static string MakePath(params string[] directories)
		{
			var s = Path.DirectorySeparatorChar.ToString();

			var path = String.Join(s, directories);
			path = (s == "/") ? path.Replace("\\", "/") : path.Replace("/", "\\");
			while (path.Contains(s + s)) path = path.Replace(s + s, s);
			if (path.EndsWith(s)) path = path.Substring(0, path.Length - 1);
			// Console.WriteLine("===> " + path);
			return path;
		}

		public static string MyEscape(string input)
		{
			return input.Replace("|", "&divider&");
		}

		public static string MyFormat(string format, params object[] args)
		{
			if (args != null && args.Length > 0)
			{
				try
				{
					return String.Format(format, args);
				}
				catch
				{
					var ret = "Error format: " + format + " ";
					foreach (var o in args) ret += "," + o;
					return ret;
				}
			}
			else return format;
		}

		public static string MyUnescape(string input)
		{
			return input.Replace("&divider&", "|");
		}

		public static void OpenWeb(String url)
		{
			try
			{
				Process.Start(url);
			}
			catch (Exception ex1)
			{
				try
				{
					var pi = new ProcessStartInfo("iexplore", url);
					Process.Start(pi);
				}
				catch (Exception ex2)
				{
					Trace.TraceError("Error opening webpage: {0}, {1}", ex2, ex1);
				}
			}
		}

		public static string PrintByteLength(long bytes)
		{
			if (bytes < 1024) return bytes.ToString();
			else if (bytes < 1024*1024) return ((double)bytes/1024).ToString("F2") + "k";
			else if (bytes < 1024*1024*1024) return ((double)bytes/1024/1024).ToString("F2") + "M";
			else return ((double)bytes/1024/1024/1024).ToString("F2") + "G";
		}


		public static string PrintTimeRemaining(long remaining, double rate)
		{
			if (rate == 0) return "?:??:??";
			var secs = (int)(remaining/rate);
			if (secs >= 360000) return "?:??:??";
			return String.Format("{0:D}:{1:D2}:{2:D2}", secs/3600, secs/60%60, secs%60);
		}

		public static void SafeStart(string path)
		{
			try
			{
				var pi = new ProcessStartInfo(path);
				pi.WorkingDirectory = Path.GetDirectoryName(path);
				pi.UseShellExecute = true;
				Process.Start(pi);
			}
			catch (Exception ex)
			{
				MessageBox.Show(path + ": " + ex.Message, "Opening failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static string UploadFile(string uploadfile,
		                                string url,
		                                string fileFormName,
		                                string contenttype,
		                                NameValueCollection querystring,
		                                CookieContainer cookies)
		{
			if (String.IsNullOrEmpty(fileFormName)) fileFormName = "file";

			if (String.IsNullOrEmpty(contenttype)) contenttype = "application/octet-stream";

			string postdata;
			postdata = "?";
			if (querystring != null) foreach (string key in querystring.Keys) postdata += key + "=" + querystring.Get(key) + "&";
			var uri = new Uri(url + postdata);

			var boundary = "----------" + DateTime.Now.Ticks.ToString("x");
			var webrequest = (HttpWebRequest)WebRequest.Create(uri);
			webrequest.CookieContainer = cookies;
			webrequest.ContentType = "multipart/form-data; boundary=" + boundary;
			webrequest.Method = "POST";

			// Build up the post message header

			var sb = new StringBuilder();
			sb.Append("--");
			sb.Append(boundary);
			sb.Append("\r\n");
			sb.Append("Content-Disposition: form-data; name=\"");
			sb.Append(fileFormName);
			sb.Append("\"; filename=\"");
			sb.Append(Path.GetFileName(uploadfile));
			sb.Append("\"");
			sb.Append("\r\n");
			sb.Append("Content-Type: ");
			sb.Append(contenttype);
			sb.Append("\r\n");
			sb.Append("\r\n");

			var postHeader = sb.ToString();
			var postHeaderBytes = Encoding.UTF8.GetBytes(postHeader);

			// Build the trailing boundary string as a byte array

			// ensuring the boundary appears on a line by itself

			var boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

			Stream requestStream;
			using (var fileStream = new FileStream(uploadfile, FileMode.Open, FileAccess.Read))
			{
				var length = postHeaderBytes.Length + fileStream.Length + boundaryBytes.Length;
				webrequest.ContentLength = length;

				requestStream = webrequest.GetRequestStream();

				// Write out our post header

				requestStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);

				// Write out the file contents

				var buffer = new Byte[checked((uint)Math.Min(4096, (int)fileStream.Length))];
				var bytesRead = 0;
				while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0) requestStream.Write(buffer, 0, bytesRead);
			}

			// Write out the trailing boundary

			requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
			var responce = webrequest.GetResponse();
			var s = responce.GetResponseStream();
			var sr = new StreamReader(s);

			return sr.ReadToEnd();
		}
	}
}