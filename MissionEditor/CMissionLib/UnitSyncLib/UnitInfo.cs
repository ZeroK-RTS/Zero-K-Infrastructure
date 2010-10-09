using System;
using System.Windows.Media;

namespace CMissionLib.UnitSyncLib
{
	[Serializable]
	public class UnitInfo : ICloneable
	{
		public UnitInfo() {}

		public UnitInfo(string name, string fullName)
		{
			Name = name;
			FullName = fullName;
		}

		public string FullName { get; set; }
		public string Name { get; set; }

		public ImageSource BuildPic { get; set; }

		#region ICloneable Members

		public object Clone()
		{
			return MemberwiseClone();
		}

		#endregion

		public override string ToString()
		{
			return FullName;
		}
	}
}