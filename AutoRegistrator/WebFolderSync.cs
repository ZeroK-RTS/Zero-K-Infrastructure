using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using ZkData;

namespace ZeroKWeb
{
    public class WebFolderSyncer
    {
        public List<string> GetFileList()
        {
            var ret = new List<string>();
            using (var wc = new WebClient())
            {
                // category=map alone makes the server filter to maps (~3570 entries today). Earlier we used
                // springname=*&logical=or which UNION'd all categories and exceeded the server's response cap
                // (~4200 entries) when limit was set high — caused 500s. See git history.
                var url = $"{GlobalConst.SpringfilesBaseUrl}json.php?category=map&limit=100000&offset=0";
                var json = wc.DownloadString(url);
                var entries = JsonConvert.DeserializeObject<List<SpringfilesEntry>>(json);
                if (entries == null) return ret;
                foreach (var e in entries)
                {
                    if (e?.path == "maps" && !string.IsNullOrEmpty(e.filename) && e.mirrors != null && e.mirrors.Length > 0)
                        ret.Add(e.filename);
                }
            }
            return ret;
        }

        public bool DownloadFile(string targetFolder, string file)
        {
            if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
            var targetFile = Path.Combine(targetFolder, file);
            if (File.Exists(targetFile)) return true;

            var tempFile = Path.GetTempFileName();
            using (var wc = new WebClient())
            {
                try
                {
                    wc.DownloadFile($"{GlobalConst.SpringfilesBaseUrl}files/maps/{file}", tempFile);
                    File.Move(tempFile, targetFile);
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Springfiles download of {0} failed: {1}", file, ex.Message);
                    try { File.Delete(tempFile); } catch { }
                    return false;
                }
            }
        }

        private class SpringfilesEntry
        {
            public string filename { get; set; }
            public string path { get; set; }
            public string[] mirrors { get; set; }
        }
    }
}
