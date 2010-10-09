using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class CreateUnitsAction : Action
	{
		ObservableCollection<UnitStartInfo> units;

		public CreateUnitsAction()
			: this(new ObservableCollection<UnitStartInfo>()) {}

		public CreateUnitsAction(IEnumerable<UnitStartInfo> units)
			: base("Create Units")
		{
			this.units = new ObservableCollection<UnitStartInfo>(units);
		}

		[DataMember]
		public ObservableCollection<UnitStartInfo> Units
		{
			get { return units; }
			set
			{
				units = value;
				RaisePropertyChanged("Units");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<string, object>
				{
					{"units", new LuaTable(units.Select(u => u.GetLuaMap(mission)).ToArray())},
				};
			return new LuaTable(map);
		}
	}
}