#region using

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PlasmaShared;

#endregion

namespace Springie
{
    public static class Program
    {
        public static Main main;
        public static DateTime startupTime = DateTime.Now;


        public static void Main(string[] args) {
            // setup unhandled exception handlers
            if (!Debugger.IsAttached) AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Thread.GetDomain().UnhandledException += Program_UnhandledException;
            Application.ThreadException += Application_ThreadException;
            Trace.Listeners.Add(new ConsoleTraceListener());

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var workPath = Application.StartupPath;
            if (workPath == "") workPath = Directory.GetCurrentDirectory();
            main = new Main(workPath);
            main.UpdateAll();

            var selfUpdater = new SelfUpdater("Springie");
            var stopProgram = false;
            selfUpdater.ProgramUpdated += s =>
                {
                    Process.Start(s);
                    stopProgram = true;
                    Environment.Exit(0);
                };
#if !DEBUG
            if (!Debugger.IsAttached) selfUpdater.StartChecking();
#endif
            while (!stopProgram) {
                Thread.Sleep(5000);
                try {
                    main.PeriodicCheck();
                } catch (Exception ex) {}
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
            Trace.TraceError("Main thread unhandled exception: {0}", e.Exception);
            if (Debugger.IsAttached) throw e.Exception;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = (Exception)e.ExceptionObject;
            Trace.TraceError("Secondary thread exception: {0}", ex);
            if (Debugger.IsAttached) throw ex;
        }

        static void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var ex = (Exception)e.ExceptionObject;
            Trace.TraceError("Main thread unhandled exception: {0}", ex);
            if (Debugger.IsAttached) throw ex;
        }
    }
}