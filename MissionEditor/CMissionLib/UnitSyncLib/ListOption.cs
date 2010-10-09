using System;

namespace CMissionLib.UnitSyncLib
{
	[Serializable]
	public class ListOption : ICloneable
	{
		public string Description;
		public string Key;
		public string Name;

		#region ICloneable Members

		public object Clone()
		{
			return MemberwiseClone();
		}

		#endregion
	}
}