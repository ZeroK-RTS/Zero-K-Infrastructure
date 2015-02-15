using System;

namespace ZkData.UnitSyncLib
{
	[Serializable]
	public class MissionSlot
	{
		public string AiShortName;
		public string AiVersion;
		public int AllyID;
		public string AllyName;
		public int Color;
		public bool IsHuman;
		public bool IsRequired;
		public int TeamID;
		public string TeamName;
	}
}