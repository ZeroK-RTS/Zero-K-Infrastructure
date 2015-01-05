using System;
using System.Security.Cryptography.X509Certificates;
using PlasmaShared.Properties;
using ZkData.UnitSyncLib;

namespace ZkData
{
    TYPO
    public enum ModeType
    {
        Local = 0, // localhost debugging
        Test = 1, // test.zero-k.info
        Live = 2, // LIVE 
    }

    public static class GlobalConst
    {
        /// <summary>
        /// Mode of operation / environment (local/test/live) - determined either by settings or by compile value
        /// </summary>
        public static readonly ModeType Mode;

        static GlobalConst()
        {
            Mode = ModeType.Local;
            #if LIVE
                Mode = ModeType.Live;
            #endif
            #if TEST
                Mode = ModeType.Test;
            #endif


            switch (Mode)
            {
                case ModeType.Local:
                    BaseSiteUrl = "http://localhost:9739";
                    ZkDataContextConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=zero-k_ef;Integrated Security=True;MultipleActiveResultSets=true";
                    SpringieNode = "alpha";

                    LobbyServerHost = "localhost";
                    LobbyServerPort = 8200;
                    break;
                case ModeType.Test:
                    BaseSiteUrl = "http://test.zero-k.info";
                    ZkDataContextConnectionString = "Data Source=omega.licho.eu,100;Initial Catalog=zero-k_test;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true";
                    SpringieNode = "omega";

                    LobbyServerHost = "lobby.zero-k.info";
                    LobbyServerPort = 8200;
                    break;
                case ModeType.Live:
                    BaseSiteUrl = "http://zero-k.info";
                    ZkDataContextConnectionString = "Data Source=omega.licho.eu,100;Initial Catalog=zero-k_ef;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true";
                    SpringieNode = "omega";

                    LobbyServerHost = "lobby.springrts.com";
                    LobbyServerPort = 8200;
                    break;
            }

            ResourceBaseUrl = string.Format("{0}/Resources", BaseSiteUrl);
            BaseImageUrl = string.Format("{0}/img/", BaseSiteUrl);
            SelfUpdaterBaseUrl = string.Format("{0}/lobby", BaseSiteUrl);
        }

        public static readonly string ZkDataContextConnectionString;

        public static readonly string BaseImageUrl;
        public static readonly string BaseSiteUrl;
        public static readonly string SpringieNode;


        public const string InfologPathFormat = @"C:\projekty\springie_spring\infolog_{0}.txt";
        public static string SiteDiskPath = @"c:\projekty\zero-k.info\www";



        public const int SteamAppID = 334920;
        public const int ZkSpringieManagedCpu = 6666;
        public const int ZkLobbyUserCpu = 6667;
        public const int ZkLobbyUserCpuLinux = 6668;
        public const int CommanderProfileCount = 6;
        public const int NumCommanderLevels = 5;

        public const string DefaultEngineOverride = "91.0"; // hack for ZKL using tasclient's engine - override here for missions etc

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
        public const int MaxClanSkilledSize = 16;
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

        public const bool VpnCheckEnabled = false; // i hope this only turns off NW spam and not VPN blacklist

        public const double EurosToKudos = 10.0;
        public const string TeamEmail = "Zero-K team <team@zero-k.info>";
        public const int NotaLobbyLinuxCpu = 9999;
        public const int NotaLobbyWindowsCpu = 9998;
        public const int NotaLobbyMacCpu = 9997;

        public const int KudosForBronze = 100;
        public const int KudosForSilver = 500;
        public const int KudosForGold = 1000;

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
        public static readonly string ResourceBaseUrl;
        public static string SelfUpdaterBaseUrl;
        public static readonly string[] DefaultDownloadMirrors = {};
        public static readonly string EngineDownloadPath = "http://springrts.com/dl/";
        public static readonly string LobbyServerHost;
        public static readonly int LobbyServerPort;

        public static bool IsZkMod(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("Zero-K");
        }
    }

    public enum PlanetWarsModes
    {
        AllOffline = 0,
        PreGame = 1,
        Running = 2
    }
}
