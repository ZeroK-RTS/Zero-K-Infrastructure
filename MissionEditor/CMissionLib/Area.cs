using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib
{
	/// <summary>
	/// Defines an area on the map (a circle or rectangle)
	/// </summary>
	[DataContract]
	public abstract class Area : PropertyChanged
	{
		public abstract LuaTable GetLuaMap(Mission mission);
	}
}
