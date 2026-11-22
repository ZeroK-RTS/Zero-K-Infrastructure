using System;
using System.Collections.Generic;
using System.IO;
namespace ZkData
{
    /// <summary>
    /// The rating constants used by the Whole History Rating core (ZkData/Ef/WHR).
    /// Split out of GlobalConst.cs, which cannot compile outside .NET Framework because it
    /// also holds an IContentServiceClient factory. These members are pure, so they can be
    /// linked into the .NET 9 test project - see Tests.Portable/Tests.Portable.csproj.
    /// </summary>
    public static partial class GlobalConst
    {
        public static int LadderAverageDays = 3;
        public static int LadderActivityDays => Mode == ModeType.Live ? 30 : 90;
        public const float EloToNaturalRatingMultiplierSquared = 0.00003313686f;

        /// <summary>whr expected player rating change over time</summary>
        public static float NaturalRatingVariancePerDay(float games) => EloToNaturalRatingMultiplierSquared * 200000 / (games + 400);

        /// <summary>whr expected player rating change per game played</summary>
        public const float NaturalRatingVariancePerGame = EloToNaturalRatingMultiplierSquared * 500;

        /// <summary>Thresholds for PlanetWars eligibility. Pure constants, lifted here
        /// with the rating ones so the lobby protocol can compile without GlobalConst.cs.
        /// </summary>
        /// <summary>Used by PayPalInterface, which Contributions/ContributionsIndex.cshtml
        /// reaches for GetItemCode. Moved here rather than copied: both stacks compile
        /// PlasmaShared, so one partial class still has one definition of each.</summary>
        /// <summary>Read by Home/HomeIndex.cshtml, three times. Pure, so they belong here.</summary>
        public static DateTime SteamRelease = new DateTime(2018, 4, 27, 8, 0, 0, DateTimeKind.Utc);
        public static bool IsLongAfterSteam => DateTime.UtcNow.Subtract(SteamRelease).TotalDays > 14;
        public static bool IsAfterSteam => DateTime.UtcNow.Subtract(SteamRelease).TotalMilliseconds > 0;

        /// <summary>Used by ResourceLinkProvider, which MapsController.Detail calls.</summary>
        public const string SpringfilesBaseUrl = "https://springfiles.springrts.com/";

        public const double EurosToKudos = 10.0;
        public const string TeamEmail = "Zero-K team <team@zero-k.info>";

        public const int MinPlanetWarsLevel = 5;
        public const int MinPlanetWarsElo = -1000;

        public const int MaxLevelForMalus = 5;
        public const float MaxMalus = 400;

        // Pure constants lifted out of GlobalConst.cs so they can be used without
        // dragging in the content-service factory and its WCF dependency.
        public const int DefaultBomberCapacity = 50;
        public const int DefaultDropshipCapacity = 50;
        public const double InfluenceDecay = 1;
        public const int InfluencePerShip = 1;
        public const int KudosForBronze = 100;
        public const int KudosForDiamond = 1000;
        public const int KudosForGold = 500;
        public const int KudosForSilver = 250;
        public const float LadderEloMaxChange = 50;
        public const float LadderEloMinChange = 1;
        public static readonly int? MaxClanSkilledSize = null;
        public const int MaxUsernameLength = 25;
        public const int MinDurationForElo = 60;
        public const int MinLevelForForumVote = 2;
        // Beside MinLevelForForumVote, which was already here; PrintPostRating reads both.
        public const bool OnlyAdminsSeePostVoters = false;
        public const double PlanetMetalPerTurn = 1;
        public const double PlanetWarsEnergyToMetalRatio = 0.0;
        public const double PlanetWarsMaximumIP = 100.0; //maximum IP on each planet
        public const double InfluenceToCapturePlanet = PlanetWarsMaximumIP / 2 + 0.1;
        public const double InfluenceToLosePlanet = 10;   // its counterpart; was in the other half
        public const bool RequireWormholeToTravel = true;
        public static int SteamContributionJarID = 2;
        public const int WikiEditLevel = 20;
        public const int XpForMissionOrBots = 25;
        public const int XpForMissionOrBotsVictory = 50;
        public static Dictionary<ulong, int> DlcToKudos = new Dictionary<ulong, int>() { { 842950, 100 }, { 842951, 250 }, { 842952, 500 } };
        public const string DefaultEngineOverride = "104.0.1-287-gf7b0fcc"; // hack for ZKL using tasclient's engine - override here for missions etc
        public const int SteamAppID = 334920;
        public const int MinDurationForXP = 240;    // seconds
        public const int LadderSize = 50; // Amount of players shown on ladders
        public const float LadderUpdatePeriod = 1; //Ladder is fully updated every X hours
        public const float LadderEloClassicEloK = 32f; //K value of classic elo
        public const float LadderEloSmoothingFactor = 0.8f; //1 for change as fast as whr, 0 for no change

        // Wanted by Razor views rather than by the rating core: moved here from
        // GlobalConst.cs so the .NET 9 projects can see them. Same class, same namespace,
        // so nothing that already used them notices.
        public const bool CanChangeClanFaction = true;
        public const string MetalIcon = "/img/luaui/ibeam.png";
        public const string EnergyIcon = "/img/luaui/energy.png";

        public const int PlanetWarsVictoryPointsToWin = 50;
        public const int PostVoteHideThreshold = -6;
        public const string NightwatchName = "Nightwatch";
        public const string LobbyAccessCookieName = "zk_lobby";
        public const string BomberIcon = "/img/fleets/neutral.png";
        public const string WarpIcon = "/img/warpcore.png";
        public const int CommanderProfileCount = 6;
        public const int MaxCommanderNameLength = 20;

        // Only the DECLARATION moves. GlobalConst.cs assigns it from the mode switch, which
        // also builds the Steam and content-service clients and cannot come across.
        public static string ZkDataContextConnectionString;
        public const int ForumPostsPerPage = 20;
        public const int MinNetKarmaToVote = -30;
    
        // Channel names. Plain strings with no dependencies; they were in GlobalConst.cs only
        // because that is where they were written. LobbyController reads ModeratorChannel four
        // times, and ChannelManager and ChatRelay read all four, so the port needs them here.
        // The query/form key the game client puts a one-use lobby session token in, so a player
        // arriving from the client is signed in without typing a password. Read by Global.asax on
        // the Framework side and by Mvc5Compat/ZkAuthentication.cs on the port's.
        // Where replays and other Springie data live on disk. ReplayStorage reads it, and
        // ZkLobbyServer hands it to SpringPaths as the writable folder that engines, maps and
        // demos are downloaded into.
        public static string SpringieDataDir { get; set; } = DefaultSpringieDataDir();

        /// <summary>
        /// The "todo hack solve" this carried was a hardcoded <c>c:\projekty\springie_spring</c>.
        ///
        /// **Windows keeps that path exactly**, because it is not a placeholder there - it is
        /// where the live server's engines and replays are, and a default that moved would point
        /// a deployed server at an empty directory and re-download everything into it.
        ///
        /// Off Windows it was never a path at all. There is no c: drive and a backslash is an
        /// ordinary filename character, so SpringPaths created a single DIRECTORY literally named
        /// <c>c:\projekty\springie_spring</c> in whatever the current working directory happened
        /// to be - which, for anything run from a checkout, is the checkout. That is where the
        /// 72MB of engine in this repository's working tree came from.
        ///
        /// ZK_SPRINGIE_DATA_DIR overrides both, in the same shape and for the same reason as
        /// ZK_CONNECTION_STRING in GlobalConst.cs: the only way to point a deployment somewhere
        /// else without editing source.
        ///
        /// Read once, into a settable property, so the two call sites keep seeing one answer.
        /// </summary>
        static string DefaultSpringieDataDir()
        {
            var configured = Environment.GetEnvironmentVariable("ZK_SPRINGIE_DATA_DIR");
            if (!string.IsNullOrEmpty(configured)) return configured;

            // PlatformID.Unix is the idiom SpringPaths itself uses to tell the two apart, rather
            // than a second one that could disagree with it.
            if (Environment.OSVersion.Platform != PlatformID.Unix) return @"c:\projekty\springie_spring";

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home)) home = Path.GetTempPath();
            return Path.Combine(home, ".local", "share", "zk-springie");
        }

        /// <summary>
        /// The directory the site keeps published resource files in - metadata, torrents,
        /// minimaps - and the URL segment they are served under. One constant because it is one
        /// directory, and it was previously spelled three ways:
        ///
        ///   PlasmaServer.StoreMetadata   MapPath("~/Resources")      the writer, so authoritative
        ///   MetaDataCache                Path.Combine(.., "resources")
        ///   MissionUpdater               SiteDiskPath + @"\resources\"   and it CREATES it
        ///   Fixer                        SiteDiskPath + @"\Resources"
        ///
        /// On NTFS those are one directory and the spread was invisible. Off Windows they are
        /// three, and the lobby server's disk lookup - the fast path it takes when it is beside
        /// the website - would never have found anything, while a mission upload would have
        /// created a second directory the site does not serve.
        /// </summary>
        public const string ResourceFolder = "Resources";

        /// <summary>
        /// The avatar images directory under img/, and the URL segment they are served from.
        ///
        /// Lower case, which is what the site itself emits everywhere - HtmlHelperExtensions,
        /// its ported twin and Unlock.ImageUrl all write /img/avatars/{code}.png. Two consumers
        /// disagreed: the game client downloaded img/Avatars/{id}.png and AutoRegistrator read
        /// img/Avatars off the site. IIS does not care and neither did anyone, until the port -
        /// PhysicalFileProvider on Linux is case-sensitive, so one of the two spellings becomes a
        /// 404 and avatars stop appearing in the lobby.
        ///
        /// SteamDepotGenerator is the tell: the same method already reads img/clans and img/factions
        /// in lower case, and both of those directories exist that way in the repository. Only
        /// Avatars was capitalised.
        ///
        /// This is the SITE's directory. The game's own LuaUI/Configs/Avatars, which
        /// SteamDepotGenerator copies into, is a different directory named by the game and keeps
        /// its capital.
        /// </summary>
        public const string AvatarFolder = "avatars";

        public const string SessionTokenVariable = "asmallcake";

        public const string ModeratorChannel = "zkadmin";
        public const string Top20Channel = "zktop20";
        public const string UserLogChannel = "zklog";
        public const string CoreChannel = "zkcore";


        // PlanetWars balance numbers. Plain constants, read by PlanetwarsController and by
        // Planet.cshtml; they were in GlobalConst.cs only because that is where they were
        // written. Moving them is what lets that controller and that view compile.
        public const double DropshipsForFullWarpIPGain = 10;
        public const double SelfDestructRefund = 0.5;
        public const double BomberKillStructureChance = 0.1;
        public const double BomberKillIpChance = 1.2;
        public const double BomberKillIpAmount = 1;
        public const int PlanetWarsMaxTeamsize = 4;


        /// <summary>Moved from GlobalConst.cs so MapBansController can be linked: that half pulls in
        /// WCF through its IContentServiceClient factory, and this is a bare integer four call sites
        /// in one controller needed.</summary>
        public const int MapBansPerPlayer = 6; // Allow users to enter this many bans in UI
}
}
