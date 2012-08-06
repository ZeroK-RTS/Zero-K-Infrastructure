  namespace ZkData
{
	public static class GlobalConst
	{
	    public const string InfologPathFormat=@"C:\springie_spring\infolog_{0}.txt";
	    public const int ZkSpringieManagedCpu = 6666;
        public const int ZkLobbyUserCpu = 6667;
	    public const string BaseImageUrl = "http://zero-k.info/img/";
	    public const double EloWeightMalusFactor = -50;
		public const int CommanderProfileCount = 6;
        public const int NumCommanderLevels = 5;

        public const int MinDurationForXP = 240;    // seconds
        public const int MinDurationForElo = 60;
	    public const int MinDurationForPlanetwars = 120;

		public const int XpForMissionOrBots = 25;
		public const int XpForMissionOrBotsVictory = 50;
		public const double EloWeightMax = 6;
		public const double EloWeightLearnFactor = 20;
		public const string AuthServiceUri = "net.tcp://localhost:8202";
		public const string LoginCookieName = "zk_login";
		public const string MissionScriptFileName = "_missionScript.txt";
		public const string MissionServiceUri = "http://zero-k.info/missions/MissionService.svc";
		public const string MissionSlotsFileName = "_missionSlots.xml"; 
		public const string NightwatchName = "Nightwatch";
		public const string PasswordHashCookieName = "zk_passwordHash";
		public const string LobbyAccessCookieName = "zk_lobby";

        public const int BaseInfluencePerBattle = 15;
	    public const double BaseMetalPerBattle = 100;
        public const double CcDestroyedMetalMultWinners = 0.75;
	    public const int DropshipsPerBattlePlayer = 1;
        public const int InfluencePerInvolvedPlayer = 1;
        public const int InfluencePerShip = 1;
        public const double InfluencePerTech = 0.5;
        public const double InfluenceDecay = 1;
	    public const double InfluenceToCapturePlanet = 50.1;
	    public const double SelfDestructRefund = 0.5;
        public const int AttackPointsForVictory = 2;
        public const int AttackPointsForDefeat = 1;
        public const int MaxClanSkilledSize = 16;
        public const int ClanLeaveLimit = 100;
	    public const double InfluenceCcKilledMultiplier = 0.5;
	    public const double BomberKillStructureChance = 0.1;
        public const double BomberKillIpChance = 0.8;
        public const double BomberKillIpAmmount = 1;
	    public const int FactionChannelMinLevel = 8;

	    public const string MetalIcon = "/img/luaui/ibeam.png";
        public const string EnergyIcon = "/img/luaui/energy.png";
        public const string BomberIcon = "/img/fleets/neutral.png";
	    public const string WarpIcon = "/img/warpcore.png";


	    public static bool IsZkMod(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("Zero-K");
        }

		public const int DefaultDropshipCapacity = 10;
        public const int DefaultBomberCapacity = 10;
	}
  
}