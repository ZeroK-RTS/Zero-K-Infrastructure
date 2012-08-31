using System;
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
        bool useOrbitalDrop = true;
        string ceg = "";

		public CreateUnitsAction()
			: this(new ObservableCollection<UnitStartInfo>()) {}

		public CreateUnitsAction(IEnumerable<UnitStartInfo> units)
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

        [DataMember]
        public bool UseOrbitalDrop
        {
            get { return useOrbitalDrop; }
            set
            {
                useOrbitalDrop = value;
                RaisePropertyChanged("UseOrbitalDrop");
            }
        }

        [DataMember]
        public string CEG
        {
            get { return ceg; }
            set
            {
                ceg = value;
                RaisePropertyChanged("CEG");
            }
        }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"units", LuaTable.CreateArray(units.Select(u => u.GetLuaMap(mission)).ToArray())},
                    {"useOrbitalDrop", useOrbitalDrop},
                    {"ceg", ceg},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Create Units";
		}
	}
}