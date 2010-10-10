using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace CMissionLib.UnitSyncLib
{
	[Serializable]
	public class UnitInfo : ICloneable
	{
		public UnitInfo()
		{
			BuildOptions = new string[0];
		}

		public string FullName { get; set; }
		public string Name { get; set; }
		public string BuildPicField { get; set; }
		public string BuildPicFileName { get; set; }
		public IEnumerable<string> BuildOptions { get; set; }

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