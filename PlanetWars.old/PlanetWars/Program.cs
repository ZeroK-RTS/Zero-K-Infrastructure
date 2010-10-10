using System;
using System.Diagnostics;
using System.Windows.Forms;
using PlanetWars.UI;
using PlanetWarsShared;
using PlanetWarsShared.Springie;


namespace PlanetWars
{
	static class Program
	{
		public static bool RestartSelf;
		public static string SelfUpdateSite = "http://planetwars.licho.eu/client/";
		public const int SelfUpdatePeriod = 60; //60 seconds
		public const string CachePath = "Cache";
		public const int GalaxyRefreshSeconds = 999999;
		public const string HeightmapCache = "Cache\\Heightmaps";
		public const string HeightmapUrl = "http://planetwars.licho.eu/heightmaps/";
		public const string MapInfoCache = "Cache\\MapInfo";
		public const string MapInfoUrl = "http://planetwars.licho.eu/mapinfo/";
		public const string MetalmapCache = "Cache\\Metalmaps";
		public const string MetalmapUrl = "http://planetwars.licho.eu/metalmaps/";
		public const string MinimapCache = "Cache\\Minimaps";
		public const string MinimapUrl = "http://planetwars.licho.eu/minimaps/";
        public const string SpringPath = @"c:\programy\hry\Spring";

		public static AuthInfo AuthInfo { get; set; }
		public static Random Random { get; set; }
		public static IServer Server { get; set; }
		public static ISpringieServer SpringieServer { get; set;}
		public static DateTime LastUpdate { get; set; }
        public static MainForm MainForm { get; set; }
		public static SelfUpdater SelfUpdater;


		[STAThread]
		static void Main()
		{
			SelfUpdater = new SelfUpdater();
			Random = new Random();
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
            MainForm = new MainForm();
            Application.Run(MainForm);
			if (RestartSelf) Process.Start(Application.ExecutablePath);
		}
	}
}