#region using

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ZkData;

#endregion

namespace Springie
{
    static class Program
    {

        public static Main main;


        public static DateTime startupTime = DateTime.Now;


        static void Main(string[] args)
        {
            // setup unhandled exception handlers
            if (!Debugger.IsAttached) AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Thread.GetDomain().UnhandledException += Program_UnhandledException;
            Application.ThreadException += Application_ThreadException;
            Trace.Listeners.Add(new ConsoleTraceListener());

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var workPath = Application.StartupPath;
            if (workPath == "") workPath = Directory.GetCurrentDirectory();
            main = new Main(workPath);

            main.StartAll();
            while (true) Thread.Sleep(5000);
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (!ErrorHandling.HandleException(e.Exception, "Main thread unhandled exception")) throw e.Exception;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            if (!ErrorHandling.HandleException(ex, "Secondary thread unhandled exception")) throw ex;
        }

        static void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            if (!ErrorHandling.HandleException(ex, "Main thread unhandled exception")) throw ex;
        }
    }
}