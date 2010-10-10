using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Browser;
using JetBrains.Annotations;
using PlanetWars.ServiceReference;

namespace PlanetWars
{
    public partial class App: Application
    {
        static Player player;
        [CanBeNull]
        Dictionary<int, Tech> Technologies { get; set; }
        [CanBeNull]
        public static string Password { get; set; }
        [CanBeNull]
        public static Player Player
        {
            get { return player; }
            set
            {
                player = value;
                PlayerChanged(null, EventArgs.Empty);
            }
        }
        public static PlanetWarsServiceClient Service { get; private set; }
        [CanBeNull]
        public static IDictionary<int, ShipType> ShipTypes { get; set; }
        [CanBeNull]
        public static IDictionary<int, StructureType> StructureTypes { get; set; }
        [CanBeNull]
        public static string UserName { get; set; }

        public static event EventHandler PlayerChanged = delegate { };

        public App()
        {
            Service = new PlanetWarsServiceClient();
            Startup += Application_Startup;
            Exit += Application_Exit;
            UnhandledException += Application_UnhandledException;
            InitializeComponent();
            Service.GetMapDataCompleted += Service_GetMapDataCompleted;
            Service.GetInvariantsCompleted += Service_GetInvariantsCompleted;
            Service.GetInvariantsAsync();
        }

        void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try {
                var errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

                HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
            } catch (Exception) {}
        }

        void Application_Exit(object sender, EventArgs e) {}

        void Application_Startup(object sender, StartupEventArgs e)
        {
            RootVisual = new MainPage();
        }

        void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!Debugger.IsAttached) {
                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
        }

        void Service_GetInvariantsCompleted(object sender, GetInvariantsCompletedEventArgs e)
        {
            ShipTypes = e.Result.ShipTypes.ToDictionary(s => s.ShipTypeID);
            StructureTypes = e.Result.StructureTypes.ToDictionary(s => s.StructureTypeID);
            Technologies = e.Result.Technologies.ToDictionary(t => t.TechID);
        }

        void Service_GetMapDataCompleted(object sender, GetMapDataCompletedEventArgs e)
        {
            e.Result.Initialize();
        }
    }
}