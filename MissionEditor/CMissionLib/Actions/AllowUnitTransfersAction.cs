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
		public AllowUnitTransfersAction() : base("Allow Unit Transfers") {}

		public override LuaTable GetLuaTable(Mission mission)
		{
			return new LuaTable();
		}
	}
}
