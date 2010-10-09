using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib
{
	[DataContract]
	public abstract class Area : PropertyChanged
	{
		public abstract LuaTable GetLuaMap(Mission mission);
	}
}
