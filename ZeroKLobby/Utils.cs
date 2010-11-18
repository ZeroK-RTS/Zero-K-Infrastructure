using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ZeroKLobby.Notifications;
using MessageBox = System.Windows.Forms.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace ZeroKLobby
{
  static class Utils
  {
    static IInputElement lastElement;
    public static bool IsDesignTime { get { return DesignerProperties.GetIsInDesignMode(new DependencyObject()); } }

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


    
    public static void CheckPath(string path, bool delete = false)
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

    public static bool VerifySpringInstalled()
    {
      if (Program.SpringPaths.SpringVersion == null)
      {
        MessageBox.Show("Cannot start yet, please wait until engine downloads", "Engine not prepared yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
      } else return true;
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

    public static void RegisterProtocol()
    {
      var executableName = Assembly.GetExecutingAssembly().Location;
      try
      {
        SetProtocolRegistry(Registry.CurrentUser.CreateSubKey("Software\\Classes\\spring"), executableName);
      }
      catch (Exception ex)
      {
        Trace.TraceError("Error registering protocol: {0}", ex);
      }

      // now try to set protocol globaly (like to fail on win7 + uac)
      try
      {
        SetProtocolRegistry(Registry.ClassesRoot, executableName);
      }
      catch {}
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

	  public static BitmapSource ToBitmapSource(this Image image)
	  {
	  	using (var bitmap = new Bitmap(image))
	  	{
	  		return bitmap.ToBitmapSource();
	  	}
	  }

    public static BitmapSource ToBitmapSource(this Bitmap bitmap)
    {
      var hBitmap = bitmap.GetHbitmap();
      BitmapSource bitmapSource;
      try
      {
        bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        bitmapSource.Freeze();
      }
      finally
      {
        DeleteObject(hBitmap);
      }
      return bitmapSource;
    }

    public static void UnregisterProtocol()
    {
      try
      {
        Registry.ClassesRoot.DeleteSubKeyTree("spring");
      }
      catch (Exception e)
      {
        Trace.TraceWarning("Unable to unregister spring protocol: " + e.Message);
      }
      return;
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

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);


    static void SetProtocolRegistry(RegistryKey protocolKey, string executableName)
    {
      protocolKey.SetValue("", "URL:Spring Action");
      protocolKey.SetValue("URL Protocol", "");
      var defaultIconKey = protocolKey.CreateSubKey("DefaultIcon");
      defaultIconKey.SetValue("", executableName);
      var shellKey = protocolKey.CreateSubKey("shell");
      var openKey = shellKey.CreateSubKey("open");
      var commandKey = openKey.CreateSubKey("command");
      commandKey.SetValue("", string.Format("\"{0}\" \"%1\"", executableName));
    }
  }
}