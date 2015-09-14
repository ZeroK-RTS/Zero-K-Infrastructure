using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ZeroKLobby
{
    public static class ErrorHandling
    {
        public static bool HasFatalException { get; private set; }
        public static void HandleException(Exception e, bool isCrash) {
            if (isCrash) HasFatalException = true;
            HandleException((isCrash ? "FATAL: " : "") + e, isCrash);
        }

        static void HandleException(string e, bool sendWeb) {
            try {
                Trace.TraceError(e);
                
                // write to error log
                string logPath = Utils.MakePath(Program.StartupPath, Config.LogFile);

                // if our log file is too big, back it up and clear
                if (File.Exists(logPath))
                {
                    FileInfo info = new FileInfo(logPath);
                    if (info.Length > 50 * 1024 * 1024) // 50 MB
                    {
                        /*  // needlessly complicated
                        bool copySuccess = false;
                        for (int i = 0; i < 999; i++)
                        {
                            string newPath = Utils.MakePath(Program.StartupPath, Path.GetFileNameWithoutExtension(Config.LogFile) + "_" + i + Path.GetExtension(Config.LogFile));
                            bool exists = File.Exists(newPath);
                            if (!exists)
                            {
                                File.Copy(logPath, newPath);
                                copySuccess = true;
                                break;
                            }
                        }
                        */
                        string newPath = Utils.MakePath(Program.StartupPath, Path.GetFileNameWithoutExtension(Config.LogFile) + "_backup" + Path.GetExtension(Config.LogFile));
                        File.Copy(logPath, newPath, true);
                        File.Delete(logPath);
                    }
                }
                using (var s = File.AppendText(logPath))
                {
                    s.WriteLine("===============\r\n{0}\r\n{1}\r\n", DateTime.Now.ToString("g"), e);
                }
            } catch {}
        }
    }
}