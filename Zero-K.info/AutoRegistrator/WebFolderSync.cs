using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ZeroKWeb
{
    public class WebFolderSyncer
    {
        //http://api.springfiles.com/files/maps/
        private static List<string> GetFileList(string url) {
            using (var wc = new WebClient())
            {
                var str = wc.DownloadString(url);
                return (from Match m in Regex.Matches(str, "<a href=\"([^\"]+)\">([^<]+)</a>")
                    where m.Success && m.Groups[1].Value == m.Groups[2].Value
                    select m.Groups[1].Value).ToList();
            }
        }

        public void SynchronizeFolders(string urlSource, string targetFolder) {
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
            foreach (var file in GetFileList(urlSource))
            {
                var targetFile  = Path.Combine(targetFolder, file);
                if (!File.Exists(targetFile))
                {
                    using (var wc = new WebClient())
                    {
                        var tempFile = Path.GetTempFileName();
                        try
                        {
                            wc.DownloadFile(urlSource + file, tempFile);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning("Download of file {0} failed: {1}", file,ex.Message);
                            File.Delete(tempFile);
                        }
                        File.Move(tempFile, targetFile);
                    }
                }
            }


        }

    }
}
