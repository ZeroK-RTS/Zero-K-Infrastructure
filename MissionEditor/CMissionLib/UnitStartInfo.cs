using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using CMissionLib.UnitSyncLib;

namespace CMissionLib
{
	[DataContract]
	public class UnitStartInfo: Positionable
	{
		ObservableCollection<string> groups;
		double heading;
		bool isGhost;
		Player player;
		UnitInfo unitDef;
		string unitDefName;

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

		[DataMember]
		public bool IsGhost
		{
			get { return isGhost; }
			set
			{
				isGhost = value;
				RaisePropertyChanged("IsGhost");
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
		public string UnitDefName { get { return unitDefName; } set { unitDefName = value; } }

		public UnitStartInfo(UnitInfo unitDef, Player player, double x, double y): base(x, y)
		{
			this.unitDef = unitDef;
			this.player = player;
			unitDefName = unitDef.Name;
			groups = new ObservableCollection<string>();
		}

		public LuaTable GetLuaMap(Mission mission)
		{
			var map = new Dictionary<object, object>
			          {
			          	{ "unitDefName", UnitDef.Name },
			          	{ "x", mission.ToIngameX(X) },
			          	{ "y", mission.ToIngameY(Y) },
			          	{ "player", mission.Players.IndexOf(Player) },
			          	{ "groups", LuaTable.CreateSet(Groups) },
			          	{ "heading", Heading },
						{ "isGhost", isGhost},
			          };
			return new LuaTable(map);
		}

		public override string ToString()
		{
			return unitDefName;
		}
	}
}