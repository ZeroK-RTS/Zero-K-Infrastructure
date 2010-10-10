using System;

namespace PlanetWarsShared.Springie
{
	/// <summary>
	/// Note this is shared with springie, do not alter ToString
	/// </summary>
	[Serializable]
	public class EndGamePlayerInfo
	{
		public bool AliveTillEnd = true;
		public int AllyNumber;
		public int DisconnectTime;
		public string Ip = "";
		public int LeaveTime;
		public int LoseTime;
		public string Name = "";
		public bool OnVictoryTeam;
		public int Rank; // - actually rank + 1 .. starts at 1 and not 0
		public string Side = ""; // mod side
		public bool Spectator;

		/// <summary>
		/// Do not change, needed for springie stats
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string ret = "";
			ret += Name + "|" + Ip + "|" + (Spectator ? "1" : "0") + "|";
			ret += (OnVictoryTeam ? "1" : "0") + "|" + (AliveTillEnd ? "1" : "0") + "|";
			ret += DisconnectTime + "|" + LeaveTime + "|";
			ret += Side + "|" +LoseTime + "|" + AllyNumber + "|" + Rank;
			return ret;
		}

		public static long ToUnix(TimeSpan t)
		{
			if (t == TimeSpan.MinValue) {
				return 0;
			}
			return (long)t.TotalSeconds;
		}
	} ;
}