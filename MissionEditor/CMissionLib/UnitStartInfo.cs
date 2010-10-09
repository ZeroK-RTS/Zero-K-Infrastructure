using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using CMissionLib.UnitSyncLib;

namespace CMissionLib
{
	[DataContract]
	public class UnitStartInfo : Positionable
	{
		UnitInfo unitDef;
		Player player;
		string unitDefName;
		ObservableCollection<string> groups;
		double heading = 0;

		public UnitStartInfo(UnitInfo unitDef, Player player, double x, double y) : base(x, y)
		{
			this.unitDef = unitDef;
			this.player = player;
			unitDefName = unitDef.Name;
			groups = new ObservableCollection<string>();
		}

		[DataMember]
		public string UnitDefName
		{
			get { return unitDefName; }
			set { unitDefName = value; }
		}

		public UnitInfo UnitDef
		{
			get { return unitDef; }
			set
			{
				unitDef = value;
				unitDefName = unitDef.Name;
				RaisePropertyChanged("UnitDef");
				RaisePropertyChanged("UnitDefName");
			}
		}

		[DataMember]
		public Player Player
		{
			get { return player; }
			set
			{
				player = value;
				RaisePropertyChanged("Player");
			}
		}

		[DataMember]
		public ObservableCollection<string> Groups
		{
			get { return groups; }
			set
			{
				groups = value;
				RaisePropertyChanged("Groups");
			}
		}

		[DataMember]
		public double Heading
		{
			get { return heading; }
			set
			{
				heading = value;
				RaisePropertyChanged("Heading");
			}
		}

		public override string ToString()
		{
			return unitDefName;
		}

		public LuaTable GetLuaMap(Mission mission)
		{
			var map = new Dictionary<string, object>
			{
				{"unitDefName", UnitDef.Name},
				{"x", mission.ToIngameX(X)},
				{"y", mission.ToIngameY(Y)},
				{"player", mission.Players.IndexOf(Player)},
				{"groups", new LuaTable(Groups)},
				{"heading", Heading}
			};
			return new LuaTable(map);
		}

	}
}