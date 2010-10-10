#region using

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;

#endregion

namespace Springie
{
    public class ErrorHandling
    {
        const string LogFile = "springie_errors.txt";
        const string ReportUrl = "http://springie.licho.eu/error.php";

        /// <summary>
        /// Handles one exception (saves to log file and sends to website)
        /// </summary>
        /// <param name="e">exception to be handled</param>
        /// <param name="moreInfo">additional information</param>
        /// <returns>returns true if exception was handled, false if called should rethrow</returns>
        public static bool HandleException(Exception e, string moreInfo)
        {
            try
            {
                // write to error log
                StreamWriter s = File.AppendText(Program.main.RootWorkPath + "/" + LogFile);
                string extest = string.Format("===============\r\n{0}\r\n{1}\r\n{2}\r\n", DateTime.Now.ToString("g"), moreInfo, e);
                s.WriteLine(extest);
                s.Close();
                Console.WriteLine(extest);

                // send to error gathering site
                var wc = new WebClient();
                string urtext = string.Format("{0}?username={1}&springie={2}&moreinfo={3}&exception={4}",
                                              ReportUrl,
                                              "multihost",
                                              MainConfig.SpringieVersion,
                                              moreInfo + "",
                                              e);
                wc.DownloadString(new Uri(urtext));
            }
            catch {}

            if (Debugger.IsAttached) return false;
            else return true;
        }
    }
}