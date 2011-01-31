namespace ZkData
{
	public static class GlobalConst
	{
	  public const double EloWeightMax = 6;
    public const double EloWeightLearnFactor = 20;
	  public const string AuthServiceUri = "net.tcp://localhost:8202";
		public const string LoginCookieName = "zk_login";
    public const string LimitedModeCookieName = "zk_limited";
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
	}
  
}