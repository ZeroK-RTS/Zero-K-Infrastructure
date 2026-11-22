using System;
using System.Collections.Generic;
using System.ComponentModel;
using PlasmaShared;

namespace ZkData
{
    public static partial class GlobalConst
    {
        static ModeType mode;

        /// <summary>
        /// Mode of operation / environment (local/test/live) - determined either by settings or by compile value
        /// </summary>
        public static ModeType Mode
        {
            get { return mode; }
            set { SetMode(value);}
        }

        static GlobalConst()
        {
#if LIVE
                Mode = ModeType.Live;
#elif TEST
                Mode = ModeType.Test;
#else
            Mode = ModeType.Local;
#endif
        }

        static void SetMode(ModeType newMode)
        {
            switch (newMode) {
                case ModeType.Local:
                    BaseSiteUrl = "https://localhost:44301";
                    ZkDataContextConnectionString = @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=zero-k_local;Integrated Security=True;MultipleActiveResultSets=true;Min Pool Size=5;Max Pool Size=2000";

                    LobbyServerHost = "localhost";
                    LobbyServerPort = 8200;

                    OldSpringLobbyPort = 7000;
                    UdpHostingPortStart = 8452;
                    AutoMigrateDatabase = true;
                    break;
                case ModeType.Test:
                    BaseSiteUrl = "http://test.zero-k.info";
                    ZkDataContextConnectionString =
                        "Data Source=test.zero-k.info;Initial Catalog=zero-k_test;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true;Min Pool Size=5;Max Pool Size=2000;";

                    LobbyServerHost = "test.zero-k.info";
                    LobbyServerPort = 8202;

                    OldSpringLobbyPort = 7000;

                    UdpHostingPortStart = 7452;
                    AutoMigrateDatabase = false;
                    break;
                case ModeType.Live:
                    BaseSiteUrl = "http://zero-k.info";
                    ZkDataContextConnectionString =
                        "Data Source=zero-k.info;Initial Catalog=zero-k;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true;Min Pool Size=5;Max Pool Size=2000;";
                    
                    LobbyServerHost = "zero-k.info";
                    LobbyServerPort = 8200;

                    OldSpringLobbyPort = 8200;

                    UdpHostingPortStart = 8452;
                    AutoMigrateDatabase = false;
                    break;
            }

            // A local or CI database is not at any of the addresses above - see
            // db/docker-compose.yml. This is the only way to point the application at one
            // without editing source, and it is read after the mode has been applied so it
            // overrides whichever mode is in force.
            var connectionOverride = Environment.GetEnvironmentVariable("ZK_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(connectionOverride)) ZkDataContextConnectionString = connectionOverride;

            // Which site, for a process that is not it. The mode above picks a BaseSiteUrl per
            // deployment, and that is right for the website itself and for tools run beside it.
            // It is not right for a lobby server in another container, which reads map and game
            // metadata from ResourceBaseUrl and would otherwise ask localhost:44301 - the Local
            // mode's address for a site that is not there.
            //
            // Read after the mode has been applied, so it overrides whichever one is in force -
            // the same shape and the same placement as ZK_CONNECTION_STRING above.
            var siteUrlOverride = Environment.GetEnvironmentVariable("ZK_BASE_SITE_URL");
            if (!string.IsNullOrEmpty(siteUrlOverride)) BaseSiteUrl = siteUrlOverride.TrimEnd('/');

            ResourceBaseUrl = string.Format("{0}/{1}", BaseSiteUrl, ResourceFolder);
            BaseImageUrl = string.Format("{0}/img/", BaseSiteUrl);
            SelfUpdaterBaseUrl = string.Format("{0}/lobby", BaseSiteUrl);

            mode = newMode;
        }

        public static string OldSpringLobbyHost = "lobby.springrts.com";
        public static int OldSpringLobbyPort;
        



        public static string BaseImageUrl;
        public static string BaseSiteUrl;


        public static string DefaultZkTag => Mode == ModeType.Live ? "zk:stable" : "zk:test";
        public static string DefaultChobbyTag => Mode == ModeType.Live ? "zkmenu:stable" : "zkmenu:test";


        /// <summary>
        /// The site's own directory on disk - the one holding resources/ and img/. Read by
        /// MetaDataCache (map and game metadata), MissionUpdater, AutoRegistrator and Fixer.
        ///
        /// **The website overwrites this at startup**, in Global.asax's Application_Start:
        /// <c>GlobalConst.SiteDiskPath = MapPath("~")</c>. So inside the web application the
        /// default below never applies, and it never had to be right.
        ///
        /// It applies to every OTHER process, and that set is growing. The lobby server reads it
        /// through MetaDataCache.ServerGetMod/ServerGetMap when a battle opens - which worked
        /// only because the lobby server ran INSIDE the website's IIS worker and inherited that
        /// assignment. A standalone one does not, on any platform, and off Windows this string is
        /// not a path at all: no c: drive, and a backslash is an ordinary filename character.
        ///
        /// ZK_SITE_DISK_PATH sets it, in the same shape and for the same reason as
        /// ZK_CONNECTION_STRING below: the only way to point a deployment at the right directory
        /// without editing source. Off Windows there is no sensible guess - the site's directory
        /// is wherever it was deployed - so the default is empty rather than invented, and
        /// MetaDataCache says so instead of quietly finding nothing.
        /// </summary>
        public static string SiteDiskPath = DefaultSiteDiskPath();

        static string DefaultSiteDiskPath()
        {
            var configured = Environment.GetEnvironmentVariable("ZK_SITE_DISK_PATH");
            if (!string.IsNullOrEmpty(configured)) return configured;

            // Kept exactly on Windows: AutoRegistrator and Fixer are run by hand from a checkout
            // there, and this is the path they have always meant.
            if (Environment.OSVersion.Platform != PlatformID.Unix) return @"c:\projekty\zero-k.info\www";

            return "";
        }


        public const int ZkLobbyUserCpu = 6667;
        public const int ZkLobbyUserCpuLinux = 6668;
        public const int NumCommanderLevels = 5;


        public const int MinDurationForPlanetwars = 0;
        public const int MaxDurationForPlanetwars = 60*60*3; // 3 hours


        public const float MaximumPercentageOfBannedMaps = 0.75f; // Do not ban more than 75% of all maps regardless of player or ban count


        public const double EloWeightMax = 6;
        public const double EloWeightLearnFactor = 10;
        public const double EloWeightMalusFactor = -80;


        public const string MissionScriptFileName = "_missionScript.txt";
        public const string MissionSlotsFileName = "_missionSlots.xml";


        // The four channel names moved to GlobalConst.Portable.cs, which the .NET 9 port links.
        

        public const int VictoryPointDecay = 1;
        public const int BaseInfluencePerBattle = 32;
        public const int InfluencePerAttacker = 1;
        public const double PlanetWarsAttackerMetal = 100;
        public const double PlanetWarsDefenderMetal = 100;
        public const double InfluencePerTech = 1;
        public const double StructureIngameDisableTimeMult = 2;
        public const int AttackPointsForVictory = 2;
        public const int AttackPointsForDefeat = 1;
        public const int FactionChannelMinLevel = 2;
        public const bool RotatePWMaps = false;
        public const double MaxPwEloDifference = 120;



        public const bool VpnCheckEnabled = true; 



        public const int PlanetWarsMinutesToAttackIfNoOption = 2;
        public const int PlanetWarsDropshipsStayForMinutes = 2*60;
        public const double PlanetWarsDefenderWinKillCcMultiplier = 0.2;
        public const double PlanetWarsAttackerWinLoseCcMultiplier = 0.5;


        public const int TcpLingerStateSeconds = 5;
        public const bool TcpLingerStateEnabled = true;

        public const int DelugeChannelDisplayUsers = 40;

        public const int LobbyThrottleBytesPerSecond = 2000;
        public const int LobbyMaxMessageSize = 2000;
        public const int MillisecondsPerCharacter = 50; //Maximum allowed chat messaging rate before it is considered spam, 80ms is equivalent to 120 WPM, which covers typing speeds of anyone short of a stenographer.
        public const int MinMillisecondsBetweenMessages = 1000; //Disallow sending more than one message per this interval


        public static int UdpHostingPortStart;

        public static string ResourceBaseUrl;
        public static string SelfUpdaterBaseUrl;
        public static string LobbyServerHost;
        public static int LobbyServerPort;
        public static bool LobbyServerUpdateSpectatorsInstantly = false;

        public static bool AutoMigrateDatabase { get; private set; }

        const string tokenPart = "9wqN1H1ojO";

        public static string CrashReportGithubToken = "ghp_LN6hibzKlqv8UOWUAf8SWgjsMn" + tokenPart;

        public static string GameAnalyticsGameKey = "5197842fb91cbc18a7291436337232af";
        private const string tokenPart2 = "68b318aa1f701165";
        public static string GameAnalyticsToken = "9a815450a" + "0058bc6" + "4812a4d9" + tokenPart2;

        public const string ZeroKDiscordID = "389176180877688832";

        private static IContentServiceClient contentServiceClientOverride;
        public static IContentServiceClient GetContentService()
        {
            return contentServiceClientOverride ?? new ContentServiceClient(BaseSiteUrl + "/ContentService");
        }
        
        public static void OverrideContentServiceClient(IContentServiceClient client)
        {
            contentServiceClientOverride = client;
        }
        

        public static string UnitSyncEngine = "unitsync";



    }

}
