  namespace ZkData
{
	public static class GlobalConst
	{
	    public const string InfologPathFormat=@"C:\springie_spring\infolog_{0}.txt";
	    public const int ZkSpringieManagedCpu = 6666;
        public const int ZkLobbyUserCpu = 6667;
	    public const bool GiveUnclannedInfluenceToClanned = false;
	    public const int NotInvolvedIpSell = 20;
	    public const string BaseImageUrl = "http://zero-k.info/img/";
	    public const double EloWeightMalusFactor = -50;
		public const int CommanderProfileCount = 6;
        public const int NumCommanderLevels = 5;

        public const int MinDurationForXP = 240;    // seconds
        public const int MinDurationForElo = 60;
	    public const int MinDurationForPlanetwars = 10;

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

        public const int MaxInfluencePerPlanet = 100;
        public const int BaseInfluencePerBattle = 15;
	    public const double BaseMetalPerBattle = 100;
	    public const int DropshipsPerBattlePlayer = 1;
        public const int InfluencePerInvolvedPlayer = 2;
        public const int InfluencePerShip = 2;
        public const double InfluencePerTech = 0.5;
	    public const int InfluenceToCapturePlanet = 51;
        public const int AttackPointsForVictory = 2;
        public const int AttackPointsForDefeat = 1;
        public const int MaxClanSkilledSize = 16;
        public const int ClanLeaveLimit = 100;
	    public const double InfluenceCcKilledMultiplier = 0.5;

	    public static bool IsZkMod(string name)
    {
      if (string.IsNullOrEmpty(name)) return false;
      return name.Contains("Zero-K");
    }

		public const int DefaultDropshipCapacity = 3;
	}
  
}