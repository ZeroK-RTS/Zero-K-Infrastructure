using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib.Actions
{
	[DataContract]
	public class AllowUnitTransfersAction : Action
	{

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}

		public override string GetDefaultName()
		{
			return "Allow Unit Transfers";
		}
	}
}
