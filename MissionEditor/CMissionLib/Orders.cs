using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib
{
	public interface IOrder
	{
		[DataMember]
		string OrderType { get; }

		LuaTable GetLuaMap(Mission mission);
	}

	public abstract class OrderTypeIcon : IOrder
	{
		public abstract string OrderType { get; }
		public LuaTable GetLuaMap(Mission mission)
		{
			return new LuaTable(new object[0]);
		}
	}

	[DataContract]
	public abstract class OrderTypeIconMode : IOrder
	{
		public OrderTypeIconMode(int mode)
		{
			Mode = mode;
		}

		[DataMember]
		public int Mode { get; set; }

		public abstract string OrderType { get; }
		public LuaTable GetLuaMap(Mission mission)
		{
			var map = new Dictionary<string, object>
				{
					{"orderType", OrderType},
					{"args", new LuaTable(new object[] { Mode })},
				};
			return new LuaTable(map);
		}
	}

	[DataContract]
	public abstract class OrderTypeIconMap : Positionable, IOrder
	{
		protected OrderTypeIconMap(double x, double y) : base(x, y)
		{
		}

		public abstract string OrderType { get; }

		public LuaTable GetLuaMap(Mission mission)
		{
			var args = new LuaTable(new object[] { mission.ToIngameX(X), 0, mission.ToIngameY(Y) });
			var map = new Dictionary<string, object>
				{
					{"orderType", OrderType},
					{"args", args},
				};
			return new LuaTable(map);
		}
	}


	[DataContract]
	public class RepeatOrder : OrderTypeIconMode, IOrder
	{
		public RepeatOrder(int mode) : base(mode)
		{
		}

		public string Name
		{
			get { return (Mode == 1 ? "Enable" : "Disable") + "Repeat Mode"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public override string OrderType
		{
			get { return "REPEAT"; }
		}
	}

	[DataContract]
	public class MoveOrder : OrderTypeIconMap, IOrder
	{
		public string Name
		{
			get { return "Move"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public MoveOrder(double x, double y) : base(x, y)
		{
		}

		public override string OrderType
		{
			get { return "MOVE"; }
		}
	}

	[DataContract]
	public class StopOrder : OrderTypeIcon
	{
		public string Name
		{
			get { return "Stop"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public override string OrderType
		{
			get { return "STOP"; }
		}
	}

	[DataContract]
	public class PatrolOrder : OrderTypeIconMap, IOrder
	{
		public string Name
		{
			get { return "Patrol"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public PatrolOrder(double x, double y)
			: base(x, y)
		{
		}

		public override string OrderType
		{
			get { return "PATROL"; }
		}
	}

	[DataContract]
	public class FightOrder : OrderTypeIconMap, IOrder
	{
		public string Name
		{
			get { return "Fight"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public FightOrder(double x, double y)
			: base(x, y)
		{
		}

		public override string OrderType
		{
			get { return "FIGHT"; }
		}
	}

	[DataContract]
	public class AttackOrder : OrderTypeIconMap, IOrder
	{
		public string Name
		{
			get { return "Attack"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public AttackOrder(double x, double y)
			: base(x, y)
		{
		}

		public override string OrderType
		{
			get { return "ATTACK"; }
		}
	}
}