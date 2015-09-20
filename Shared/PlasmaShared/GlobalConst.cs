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
                        @"Data Source=.\;Initial Catalog=zero-k_local;Integrated Security=True;MultipleActiveResultSets=true";
                    SpringieNode = "alpha";

                    LobbyServerHost = "localhost";
                    LobbyServerPort = 8200;

                    OldSpringLobbyPort = 7000;
                    break;
                case ModeType.Test:
                    BaseSiteUrl = "http://test.zero-k.info";
                    ZkDataContextConnectionString =
                        "Data Source=test.zero-k.info;Initial Catalog=zero-k_test;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true";
                    SpringieNode = "omega";

                    LobbyServerHost = "test.zero-k.info";
                    LobbyServerPort = 8202;

                    OldSpringLobbyPort = 7000;
                    break;
                case ModeType.Live:
                    BaseSiteUrl = "http://zero-k.info";
                    ZkDataContextConnectionString =
                        "Data Source=zero-k.info;Initial Catalog=zero-k;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true";
                    SpringieNode = "omega";

                    LobbyServerHost = "zero-k.info";
                    LobbyServerPort = 8200;

                    OldSpringLobbyPort = 8200;
                    break;
            }

            ResourceBaseUrl = string.Format("{0}/Resources", BaseSiteUrl);
            BaseImageUrl = string.Format("{0}/img/", BaseSiteUrl);
            SelfUpdaterBaseUrl = string.Format("{0}/lobby", BaseSiteUrl);

            contentServiceFactory = new ChannelFactory<IContentService>(CreateBasicHttpBinding(), string.Format("{0}/ContentService.svc", BaseSiteUrl));
            springieServiceFactory = new ChannelFactory<ISpringieService>(CreateBasicHttpBinding(), string.Format("{0}/SpringieService.svc", BaseSiteUrl));
            
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
        public static string SpringieNode;


        public const string InfologPathFormat = @"C:\projekty\springie_spring\infolog_{0}.txt";
        public static string SiteDiskPath = @"c:\projekty\zero-k.info\www";



        public const int SteamAppID = 334920;
        public const int ZkSpringieManagedCpu = 6666;
        public const int ZkLobbyUserCpu = 6667;
        public const int ZkLobbyUserCpuLinux = 6668;
        public const int CommanderProfileCount = 6;
        public const int NumCommanderLevels = 5;

        public const string DefaultEngineOverride = "100.0"; // hack for ZKL using tasclient's engine - override here for missions etc

        public const int MinDurationForXP = 240;    // seconds
        public const int MinDurationForElo = 60;
        public const int MinDurationForPlanetwars = 0;

        public const int XpForMissionOrBots = 25;
        public const int XpForMissionOrBotsVictory = 50;

        public const double EloWeightMax = 6;
        public const double EloWeightLearnFactor = 30;
        public const double EloWeightMalusFactor = -80;

        public const int LevelForElevatedSpringieRights = 20;
        public const int SpringieBossEffectiveRights = 3;

        public const string LoginCookieName = "zk_login";
        public const string ASmallCakeCookieName = "asmallcake";
        public const string ASmallCakeLoginCookieName = "alogin";

        public const string MissionScriptFileName = "_missionScript.txt";
        public const string MissionSlotsFileName = "_missionSlots.xml";

        public const string NightwatchName = "Nightwatch";
        public const string ModeratorChannel = "zkadmin";
        public const string Top20Channel = "zktop20";

        public const string PasswordHashCookieName = "zk_passwordHash";
        public const string LobbyAccessCookieName = "zk_lobby";

        public const double PlanetMetalPerTurn = 1;
        public const double PlanetWarsEnergyToMetalRatio = 1 / 20.0;
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
        public const int DefaultDropshipCapacity = 10;
        public const int DefaultBomberCapacity = 10;
        public const int AttackPointsForVictory = 2;
        public const int AttackPointsForDefeat = 1;
        public const int MaxClanSkilledSize = 999;
        public const int ClanLeaveLimit = 100;
        public const int FactionChannelMinLevel = 10;
        public const bool RotatePWMaps = false;
        public const bool RequireWormholeToTravel = true;
        public const bool CanChangeClanFaction = true;
        public const double MaxPwEloDifference = 120;

        public const PlanetWarsModes PlanetWarsMode = PlanetWarsModes.AllOffline;

        public const string MetalIcon = "/img/luaui/ibeam.png";
        public const string EnergyIcon = "/img/luaui/energy.png";
        public const string BomberIcon = "/img/fleets/neutral.png";
        public const string WarpIcon = "/img/warpcore.png";

        public const bool VpnCheckEnabled = true; 

        public const double EurosToKudos = 10.0;
        public const string TeamEmail = "Zero-K team <team@zero-k.info>";
        public const int NotaLobbyLinuxCpu = 9999;
        public const int NotaLobbyWindowsCpu = 9998;
        public const int NotaLobbyMacCpu = 9997;

        public const int KudosForBronze = 100;
        public const int KudosForSilver = 500;
        public const int KudosForGold = 1000;
        public const int KudosForDiamond = 5000;

        public const int ForumPostsPerPage = 50;
        public const int MinLevelForForumVote = 10;
        public const int MinNetKarmaToVote = -30;
        public const int PostVoteHideThreshold = -6;
        public const bool OnlyAdminsSeePostVoters = false;
        public const int PlanetWarsMinutesToAttack = 30;
        public const int PlanetWarsMinutesToAccept = 10;
        public const int PlanetWarsMaxTeamsize = 4;
        public const int MinPlanetWarsLevel = 10;
        public const int MinPlanetWarsElo = 1000;
        
        public const int LobbyProtocolPingInterval = 30;
        public const int LobbyProtocolPingTimeout = 60;

        public static string ResourceBaseUrl;
        public static string SelfUpdaterBaseUrl;
        public static readonly string[] DefaultDownloadMirrors = {};
        public static readonly string EngineDownloadPath = "http://springrts.com/dl/";
        public static string LobbyServerHost;
        public static int LobbyServerPort;

        public static bool IsZkMod(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("Zero-K");
        }


        static ChannelFactory<IContentService> contentServiceFactory;
        static ChannelFactory<ISpringieService> springieServiceFactory;

        public static IContentService GetContentService()
        {
            return contentServiceFactory.CreateChannel();
        }

        public static ISpringieService GetSpringieService()
        {
            return springieServiceFactory.CreateChannel();
        }

    }

    public enum PlanetWarsModes
    {
        AllOffline = 0,
        PreGame = 1,
        Running = 2
    }
}
