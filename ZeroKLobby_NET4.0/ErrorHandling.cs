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
                // write to error log
                using (var s = File.AppendText(Utils.MakePath(Program.StartupPath, Config.LogFile))) s.WriteLine("===============\r\n{0}\r\n{1}\r\n", DateTime.Now.ToString("g"), e);
                Trace.TraceError(e);
            } catch {}
        }
    }
}