using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using ZkData;

namespace ZeroKLobby
{
    static class Utils
    {
        public static Dictionary<string, Color> FactionColors = new Dictionary<string, Color>()
        {
            { "Ascended", ColorTranslator.FromHtml("#94C8FF") },
            { "Trueborn", ColorTranslator.FromHtml("#00FF00") },
            { "Machines", ColorTranslator.FromHtml("#FF0000") },
            { "Empire", ColorTranslator.FromHtml("#6010FF") },
            { "Unaligned", ColorTranslator.FromHtml("#DDBB00") },
            // PW11 new factions
            { "Cybernetic", ColorTranslator.FromHtml("#88AAFF") },
            //{ "Dynasty", ColorTranslator.FromHtml("#FFAA20") },   // old color
            { "Liberty", ColorTranslator.FromHtml("#3ACA20") },
            
            { "SynPact", ColorTranslator.FromHtml("#5388EB") },
            { "Dynasty", ColorTranslator.FromHtml("#FFBF00") },

            { "Hegemony", ColorTranslator.FromHtml("#8048FF") },
            { "Rising", ColorTranslator.FromHtml("#A7D224") },
        };

        public static bool CanRead(string filename) {
            if (!File.Exists(filename)) return true;
            try {
                using (var f = File.Open(filename, FileMode.Open, FileAccess.Read)) {}
                return true;
            } catch {
                return false;
            }
        }




        public static void CheckPath(string path, bool delete = false) {
            if (delete) {
                try {
                    Directory.Delete(path, true);
                } catch {}
            }
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        public static void CreateDesktopShortcut(string name = "Zero-K") {
            try {
                var deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                { //Reference: http://xmodulo.com/create-desktop-shortcut-launcher-linux.html
                  //"file://" protocol: http://en.wikipedia.org/wiki/File_URI_scheme#Windows_2
                    using (var writer = new StreamWriter(Path.Combine(deskDir, name + ".desktop"))) {
                        var app = Assembly.GetEntryAssembly().Location;
                        writer.WriteLine("[Desktop Entry]");
                        writer.WriteLine("Version=1.0");
                        writer.WriteLine("Type=Link");
                        writer.WriteLine("Name=Zero-K");
                        writer.WriteLine("URL=file://" + app);
                        writer.WriteLine("Comment=Zero-K Lobby");
                        //var icon = app.Replace('\\', '/');
                        //writer.WriteLine("Icon=" + icon);
                        writer.Flush();
                    }
                }
                else
                {
                    using (var writer = new StreamWriter(Path.Combine(deskDir, name + ".url"))) {
                        var app = Assembly.GetEntryAssembly().Location;
                        writer.WriteLine("[InternetShortcut]");
                        writer.WriteLine("URL=file://" + app);
                        writer.WriteLine("IconIndex=0");
                        var icon = app.Replace('\\', '/');
                        writer.WriteLine("IconFile=" + icon);
                        writer.Flush();
                    }
                }
            } catch (Exception ex) {
                Trace.TraceWarning("Error creating a desktop shortcut: {0}", ex.Message);
            }
        }

        public static Color GetFactionColor(string faction) {
            Color color;
            if (faction != null && FactionColors.TryGetValue(faction, out color)) return color;
            return Color.Black;
        }

        public static Control GetHoveredControl(this Control parent) {
            var thisControl = parent;
            var globalPos = Control.MousePosition;
            var relativePos = thisControl.PointToClient(globalPos);

            if (!thisControl.DisplayRectangle.Contains(relativePos)) return null;
            Control child;
            while ((child = thisControl.GetChildAtPoint(relativePos,
                                               GetChildAtPointSkip.Disabled | 
                                               GetChildAtPointSkip.Invisible)) != null)
                                               //| GetChildAtPointSkip.Transparent)) != null) //this hide tooltip for BitmapButton on Linux!
            {
                thisControl = child;
                relativePos = thisControl.PointToClient(globalPos);
            }
            return thisControl;
        }

        public static string MakePath(params string[] directories) {
            var s = Path.DirectorySeparatorChar.ToString();

            var path = String.Join(s, directories);
            path = (s == "/") ? path.Replace("\\", "/") : path.Replace("/", "\\");
            while (path.Contains(s + s)) path = path.Replace(s + s, s);
            if (path.EndsWith(s)) path = path.Substring(0, path.Length - 1);
            // Console.WriteLine("===> " + path);
            return path;
        }

        public static void OpenWeb(String url, bool openInternal) {
            if (url.StartsWith(GlobalConst.BaseSiteUrl)) {
                if (openInternal) Program.MainWindow.navigationControl.Path = url;
                else Program.BrowserInterop.OpenUrl(url);
                return;
            } 
            try {
                Process.Start(url);
            } catch (Exception ex1) {
                try {
                    var pi = new ProcessStartInfo("iexplore", url);
                    Process.Start(pi);
                } catch (Exception ex2) {
                    Trace.TraceError("Error opening webpage: {0}, {1}", ex2, ex1);
                }
            }
        }

        public static string PrintByteLength(long bytes) {
            if (bytes < 1024) return bytes.ToString();
            else if (bytes < 1024*1024) return ((double)bytes/1024).ToString("F2") + "k";
            else if (bytes < 1024*1024*1024) return ((double)bytes/1024/1024).ToString("F2") + "M";
            else return ((double)bytes/1024/1024/1024).ToString("F2") + "G";
        }


        public static string PrintTimeRemaining(long remaining, double rate) {
            if (rate == 0) return "?:??:??";
            var secs = (int)(remaining/rate);
            if (secs >= 360000) return "?:??:??";
            return String.Format("{0:D}:{1:D2}:{2:D2}", secs/3600, secs/60%60, secs%60);
        }


        public static void SafeStart(string path, string args = null) {
            try {
                var pi = new ProcessStartInfo(path, args);
                pi.WorkingDirectory = Path.GetDirectoryName(path);
                pi.UseShellExecute = true;
                Process.Start(pi);
            } catch (Exception ex) {
                MessageBox.Show(path + ": " + ex.Message, "Opening failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        public static void UnregisterProtocol() {
            try {
                Registry.ClassesRoot.DeleteSubKeyTree("spring");
            } catch (Exception e) {
                Trace.TraceWarning("Unable to unregister spring protocol: " + e.Message);
            }
            return;
        }

        public static string UploadFile(string uploadfile,
                                        string url,
                                        string fileFormName,
                                        string contenttype,
                                        NameValueCollection querystring,
                                        CookieContainer cookies) {
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
            using (var fileStream = new FileStream(uploadfile, FileMode.Open, FileAccess.Read)) {
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


        public static bool VerifySpringInstalled() {
            if (Program.SpringPaths.SpringVersion == null || !Program.SpringPaths.HasEngineVersion(Program.SpringPaths.SpringVersion)) {
                MessageBox.Show("Cannot start yet, please wait until engine downloads",
                                "Engine not prepared yet",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return false;
            }
            else return true;
        }


        public static void RenderControlBgImage(this Control destination, Control source, PaintEventArgs e)
        {
            var loc = source.PointToClient(destination.Parent.PointToScreen(destination.Location));
            if (source.BackgroundImage != null) e.Graphics.DrawImage(source.BackgroundImage, e.ClipRectangle, loc.X + e.ClipRectangle.X, loc.Y  + e.ClipRectangle.Y, e.ClipRectangle.Width, e.ClipRectangle.Height, GraphicsUnit.Pixel);
        }

        public static Control FindParentWithBgImage(this Control source)
        {
            var p = source.Parent;
            while (p != null) {
                if (p.BackgroundImage != null) return p;
                p = p.Parent;
            }
            return null;
        }

        public static bool RenderParentsBackgroundImage(this Control source, PaintEventArgs e)
        {
            try {
                var par = source.FindParentWithBgImage();
                if (par != null) {
                    source.RenderControlBgImage(par, e);
                    return true;
                }
                else return false;
            } catch (Exception ex) {
                Trace.TraceError("Error rendering background image: {0}",ex);
            }
            return false;
        }
    }

}
