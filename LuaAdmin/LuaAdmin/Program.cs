using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Collections;
using LuaManagerLib;
using System.Configuration;

namespace LuaAdmin
{
    static class Program
    {
#pragma warning disable 612,618
        static public WidgetBrowserAdmin fetcher = new WidgetBrowserAdmin( ConfigurationSettings.AppSettings["ServerUrl"]);
#pragma warning restore 612,618
        static public WidgetList allLuas = new WidgetList();
        static public Dictionary<string, ModInfoDb> allGames = new Dictionary<string, ModInfoDb>();
        static public Dictionary<int, Category> allCategories = new Dictionary<int, Category>();
        static public Hashtable allGameWidgets = new Hashtable();
        static public SortedList<string, WidgetName> allLuaNames = new SortedList<string, WidgetName>();
        static public int UserId = 0;
        static public bool isSuperAdmin = false;
        static public Decimal version = new Decimal(0.75);

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#pragma warning disable 612,618
            Application.Run(new WidgetAdmin(ConfigurationSettings.AppSettings["ServerUrl"]));
#pragma warning restore 612,618
        }
    }
}
