  namespace ZkData
{
	public static class GlobalConst
	{
	    public const bool GiveUnclannedInfluenceToClanned = false;
	    public const int NotInvolvedIpSell = 20;
	    public const string BaseImageUrl = "http://zero-k.info/img/";
	    public const int MinPlanetWarsLevel = 2;
	    public const double InfluenceTaxIncome = 1 / 25.0;
	    public const int InfluenceDecay = 3;
        public const double PlanetwarsRepairCost = 0.5; // repair cost is 50% only
	    public const bool ClanFreePlanets = false;
        public const double CeasefireMaxInfluenceBalanceRatio = 1.5;
	    public const double EloWeightMalusFactor = -50;
		public const int InfluenceSystemBuyPrice = 40;
		public const int InfluenceSystemSellPrice = 15;
		public const int MaxClanSkilledSize =8;
		public const int ClanLeaveLimit = 100;
		public const int CommanderProfileCount = 6;
		public const int PlanetwarsColonizationCredits = 1000;
		public const int PlanetwarsInvadingShipBonus = 30;
		public const int PlanetwarsInvadingShipLostMalus = -10;
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

    public static bool IsZkMod(string name)
    {
      if (string.IsNullOrEmpty(name)) return false;
      return name.Contains("Zero-K");
    }

		public const int DefaultDropshipCapacity = 3;
		public const int DefaultDropshipProduction = 1;
	}
  
}