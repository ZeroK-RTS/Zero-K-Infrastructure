using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            if (IsLongAfterSteam) DefaultDownloadMirrors = new[] { BaseSiteUrl +"/content/%t/%f" };

            ResourceBaseUrl = string.Format("{0}/Resources", BaseSiteUrl);
            BaseImageUrl = string.Format("{0}/img/", BaseSiteUrl);
            SelfUpdaterBaseUrl = string.Format("{0}/lobby", BaseSiteUrl);

            mode = newMode;
        }

        public static string OldSpringLobbyHost = "lobby.springrts.com";
        public static int OldSpringLobbyPort;
        


        public static string ZkDataContextConnectionString;

        public static string BaseImageUrl;
        public static string BaseSiteUrl;

        public static string DefaultZkTag => Mode == ModeType.Live ? "zk:stable" : "zk:test";
        public static string DefaultChobbyTag => Mode == ModeType.Live ? "zkmenu:stable" : "zkmenu:test";


        public static string SiteDiskPath = @"c:\projekty\zero-k.info\www";


        public const int SteamAppID = 334920;
        public const int ZkLobbyUserCpu = 6667;
        public const int ZkLobbyUserCpuLinux = 6668;
        public const int MaxUsernameLength = 25;
        public const int CommanderProfileCount = 6;
        public const int NumCommanderLevels = 5;
        public const int MaxCommanderNameLength = 20;

        public const string DefaultEngineOverride = "104.0.1-287-gf7b0fcc"; // hack for ZKL using tasclient's engine - override here for missions etc

        public const int MinDurationForXP = 240;    // seconds
        public const int MinDurationForElo = 60;
        public const int MinDurationForPlanetwars = 0;
        public const int MaxDurationForPlanetwars = 60*60*3; // 3 hours

        public static int LadderAverageDays = 3;
        public static int LadderActivityDays => mode == ModeType.Live ? 30 : 90;
        public const int LadderSize = 50; // Amount of players shown on ladders
        public const float LadderUpdatePeriod = 1; //Ladder is fully updated every X hours
        public const float EloToNaturalRatingMultiplierSquared = 0.00003313686f;
        public static float NaturalRatingVariancePerDay(float games) => EloToNaturalRatingMultiplierSquared * 200000 / (games + 400); //whr expected player rating change over time
        public const float NaturalRatingVariancePerGame = EloToNaturalRatingMultiplierSquared * 500; //whr expected player rating change per game played
        public const float LadderEloMaxChange = 50;
        public const float LadderEloMinChange = 1;
        public const float LadderEloClassicEloK = 32f; //K value of classic elo
        public const float LadderEloSmoothingFactor = 0.8f; //1 for change as fast as whr, 0 for no change
        public const int MaxLevelForMalus = 5;
        public const float MaxMalus = 400;

        public const int MapBansPerPlayer = 6; // Allow users to enter this many bans in UI
        public const float MaximumPercentageOfBannedMaps = 0.75f; // Do not ban more than 75% of all maps regardless of player or ban count

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
        public const string UserLogChannel = "zklog";
        public const string CoreChannel = "zkcore";
        
        public const string LobbyAccessCookieName = "zk_lobby";

        public const double PlanetMetalPerTurn = 1;
        public const double PlanetWarsEnergyToMetalRatio = 0.0;
        public const double PlanetWarsMaximumIP = 100.0; //maximum IP on each planet
        public const int PlanetWarsVictoryPointsToWin = 100;
        public const int VictoryPointDecay = 1;
        public const int BaseInfluencePerBattle = 35;
        public const double PlanetWarsAttackerMetal = 100;
        public const double PlanetWarsDefenderMetal = 100;
        public const int InfluencePerShip = 1;
        public const double InfluencePerTech = 1;
        public const double InfluenceDecay = 1;
        public const double InfluenceToCapturePlanet = PlanetWarsMaximumIP / 2 + 0.1;
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
        public const int FactionChannelMinLevel = 2;
        public const bool RotatePWMaps = false;
        public const bool RequireWormholeToTravel = true;
        public const bool CanChangeClanFaction = true;
        public const double MaxPwEloDifference = 120;


        public const string MetalIcon = "/img/luaui/ibeam.png";
        public const string EnergyIcon = "/img/luaui/energy.png";
        public const string BomberIcon = "/img/fleets/neutral.png";
        public const string WarpIcon = "/img/warpcore.png";

        public const bool VpnCheckEnabled = true; 

        public const double EurosToKudos = 10.0;
        public const string TeamEmail = "Zero-K team <team@zero-k.info>";

        public const int KudosForBronze = 100;
        public const int KudosForSilver = 250;
        public const int KudosForGold = 500;
        public const int KudosForDiamond = 1000;

        public const int ForumPostsPerPage = 20;
        public const int MinLevelForForumVote = 2;
        public const int MinNetKarmaToVote = -30;
        public const int PostVoteHideThreshold = -6;
        public const bool OnlyAdminsSeePostVoters = false;
        public const int PlanetWarsMinutesToAttackIfNoOption = 2;
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

        public const int DelugeChannelDisplayUsers = 40;

        public const int LobbyThrottleBytesPerSecond = 2000;
        public const int LobbyMaxMessageSize = 2000;
        public const int MillisecondsPerCharacter = 50; //Maximum allowed chat messaging rate before it is considered spam, 80ms is equivalent to 120 WPM, which covers typing speeds of anyone short of a stenographer.
        public const int MinMillisecondsBetweenMessages = 1000; //Disallow sending more than one message per this interval


        public static int UdpHostingPortStart;

        public static string ResourceBaseUrl;
        public static string SelfUpdaterBaseUrl;
        public static string[] DefaultDownloadMirrors = {};
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
        

        public static string UnitSyncEngine = "105.1.1-1485-g78f9a2c";

        public static int SteamContributionJarID = 2;
        public static Dictionary<ulong, int> DlcToKudos = new Dictionary<ulong, int>() { { 842950, 100 }, { 842951, 250 }, { 842952, 500 } };

        public static DateTime SteamRelease = new DateTime(2018, 4, 27, 8, 0, 0, DateTimeKind.Utc);
        public static bool IsLongAfterSteam => DateTime.UtcNow.Subtract(SteamRelease).TotalDays > 14;
        public static bool IsAfterSteam => DateTime.UtcNow.Subtract(SteamRelease).TotalMilliseconds > 0;

    }

    public enum PlanetWarsModes
    {
        [Description("offline")]
        AllOffline = 0,
        [Description("pre-game")]
        PreGame = 1,
        [Description("running")]
        Running = 2
    }
}
