using System;
using System.ServiceModel;
using PlasmaShared;

namespace ZkData
{
    public enum ModeType
    {
        Local = 0, // localhost debugging
        Test = 1, // test.zero-k.info
        Live = 2, // LIVE 
    }

    public static class GlobalConst
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
        public static string SpringieDataDir { get; set; } = @"c:\projekty\springie_spring"; // todo hack solve

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
                    BaseSiteUrl = "http://localhost:9739";
                    ZkDataContextConnectionString =
                        "Data Source=.;Initial Catalog=zero-k_local;Integrated Security=True;MultipleActiveResultSets=true;Min Pool Size=5;Max Pool Size=2000;";

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

            DefaultDownloadMirrors = new[] { BaseSiteUrl +"/autoregistrator/%t/%f" };
            ResourceBaseUrl = string.Format("{0}/Resources", BaseSiteUrl);
            BaseImageUrl = string.Format("{0}/img/", BaseSiteUrl);
            SelfUpdaterBaseUrl = string.Format("{0}/lobby", BaseSiteUrl);

            contentServiceFactory = new ChannelFactory<IContentService>(CreateBasicHttpBinding(), $"{BaseSiteUrl}/ContentService.svc");
            mode = newMode;
        }

        public static string OldSpringLobbyHost = "lobby.springrts.com";
        public static int OldSpringLobbyPort;
        

        public static BasicHttpBinding CreateBasicHttpBinding()
        {
            var binding = new BasicHttpBinding();
            binding.ReceiveTimeout = TimeSpan.FromHours(1);
            binding.OpenTimeout = TimeSpan.FromHours(1);
            binding.CloseTimeout = TimeSpan.FromHours(1);
            binding.SendTimeout = TimeSpan.FromHours(1);
            binding.MaxBufferSize = 6553600;
            binding.MaxBufferPoolSize = 6553600;
            binding.MaxReceivedMessageSize = 6553600;
            binding.ReaderQuotas.MaxArrayLength = 1638400;
            binding.ReaderQuotas.MaxStringContentLength = 819200;
            binding.ReaderQuotas.MaxBytesPerRead = 409600;
            binding.Security.Mode = BasicHttpSecurityMode.None;
            return binding;
        }

        public static string ZkDataContextConnectionString;

        public static string BaseImageUrl;
        public static string BaseSiteUrl;

        public static string DefaultZkTag => Mode == ModeType.Live ? "zk:stable" : "zk:test";
        public static string DefaultChobbyTag => Mode == ModeType.Live ? "zkmenu:stable" : "zkmenu:test";


        public const string InfologPathFormat = @"C:\projekty\springie_spring\infolog_{0}.txt";
        public static string SiteDiskPath = @"c:\projekty\zero-k.info\www";


        public const int SteamAppID = 334920;
        public const int ZkLobbyUserCpu = 6667;
        public const int ZkLobbyUserCpuLinux = 6668;
        public const int MaxUsernameLength = 25;
        public const int CommanderProfileCount = 6;
        public const int NumCommanderLevels = 5;
        public const int MaxCommanderNameLength = 20;

        public const string DefaultEngineOverride = "103.0"; // hack for ZKL using tasclient's engine - override here for missions etc

        public const int MinDurationForXP = 240;    // seconds
        public const int MinDurationForElo = 60;
        public const int MinDurationForPlanetwars = 0;

        public const int LadderActivityDays = 70;
        public const float MaxLadderUncertainty = 46; // < 70 days
        public const float EloDecayPerDaySquared = 30;

        public const int XpForMissionOrBots = 25;
        public const int XpForMissionOrBotsVictory = 50;

        public const double EloWeightMax = 6;
        public const double EloWeightLearnFactor = 10;
        public const double EloWeightMalusFactor = -80;

        public const string SessionTokenVariable = "asmallcake";

        public const string MissionScriptFileName = "_missionScript.txt";
        public const string MissionSlotsFileName = "_missionSlots.xml";

        public const string NightwatchName = "Nightwatch";

        public const string ModeratorChannel = "zkadmin";
        public const string Top20Channel = "zktop20";
        public const string ErrorChannel = "zkerror";
        public const string CoreChannel = "zkcore";
        
        public const string LobbyAccessCookieName = "zk_lobby";

        public const double PlanetMetalPerTurn = 1;
        public const double PlanetWarsEnergyToMetalRatio = 1 / 20.0;
        public const int PlanetWarsVictoryPointsToWin = 100;
        public const int BaseInfluencePerBattle = 35;
        public const double PlanetWarsAttackerMetal = 100;
        public const double PlanetWarsDefenderMetal = 100;
        public const int InfluencePerShip = 1;
        public const double InfluencePerTech = 1;
        public const double InfluenceDecay = 1;
        public const double InfluenceToCapturePlanet = 50.1;
        public const double InfluenceToLosePlanet = 10;
        public const double DropshipsForFullWarpIPGain = 10;
        public const double SelfDestructRefund = 0.5;
        public const double BomberKillStructureChance = 0.1;
        public const double BomberKillIpChance = 1.2;
        public const double BomberKillIpAmount = 1;
        public const double StructureIngameDisableTimeMult = 2;
        public const int DefaultDropshipCapacity = 50;
        public const int DefaultBomberCapacity = 50;
        public const int AttackPointsForVictory = 2;
        public const int AttackPointsForDefeat = 1;
        public static readonly int? MaxClanSkilledSize = null;
        public const int ClanLeaveLimit = 100;
        public const int FactionChannelMinLevel = 2;
        public const bool RotatePWMaps = false;
        public const bool RequireWormholeToTravel = true;
        public const bool CanChangeClanFaction = true;
        public const double MaxPwEloDifference = 120;


        public static PlanetWarsModes PlanetWarsMode = PlanetWarsModes.Running;

        public const string MetalIcon = "/img/luaui/ibeam.png";
        public const string EnergyIcon = "/img/luaui/energy.png";
        public const string BomberIcon = "/img/fleets/neutral.png";
        public const string WarpIcon = "/img/warpcore.png";

        public const bool VpnCheckEnabled = false; 

        public const double EurosToKudos = 10.0;
        public const string TeamEmail = "Zero-K team <team@zero-k.info>";

        public const int KudosForBronze = 100;
        public const int KudosForSilver = 500;
        public const int KudosForGold = 1000;

        public const int ForumPostsPerPage = 20;
        public const int MinLevelForForumVote = 2;
        public const int MinNetKarmaToVote = -30;
        public const int PostVoteHideThreshold = -6;
        public const bool OnlyAdminsSeePostVoters = false;
        public const int PlanetWarsMinutesToAttack = 20;
        public const int PlanetWarsMinutesToAccept = 5;
        public const int PlanetWarsDropshipsStayForMinutes = 2*60;
        public const int PlanetWarsMaxTeamsize = 4;
        public const double PlanetWarsDefenderWinKillCcMultiplier = 0.2;
        public const double PlanetWarsAttackerWinLoseCcMultiplier = 0.5;
        public const int MinPlanetWarsLevel = 5;
        public const int MinPlanetWarsElo = -1000;

        public const int WikiEditLevel = 20;

        public const int TcpLingerStateSeconds = 5;
        public const bool TcpLingerStateEnabled = true;

        public const int DelugeChannelDisplayUsers = 100;

        public const int LobbyThrottleBytesPerSecond = 2000;
        public const int LobbyMaxMessageSize = 2000;


        public static int UdpHostingPortStart;

        public static string ResourceBaseUrl;
        public static string SelfUpdaterBaseUrl;
        public static string[] DefaultDownloadMirrors = {};
        public static string LobbyServerHost;
        public static int LobbyServerPort;

        public static bool AutoMigrateDatabase { get; private set; }

        private const string tokenPart = "af27e9e18e";

        public static string CrashReportGithubToken = "fffb24b" + "91a758"+"a6a4e7a"+ "7a7eafb1a9" + tokenPart;

        public static string GameAnalyticsGameKey = "5197842fb91cbc18a7291436337232af";
        private const string tokenPart2 = "68b318aa1f701165";
        public static string GameAnalyticsToken = "9a815450a" + "0058bc6" + "4812a4d9" + tokenPart2;


        public static string[] ReplaysPossiblePaths = { @"c:\projekty\springie_spring\demos-server"};



        static ChannelFactory<IContentService> contentServiceFactory;

        public static IContentService GetContentService()
        {
            return contentServiceFactory.CreateChannel();
        }
    }

    public enum PlanetWarsModes
    {
        AllOffline = 0,
        PreGame = 1,
        Running = 2
    }
}
