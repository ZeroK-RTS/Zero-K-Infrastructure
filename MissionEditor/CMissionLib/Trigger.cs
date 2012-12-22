using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using CMissionLib.Actions;
using CMissionLib.Conditions;

namespace CMissionLib
{
	[DataContract]
	public class Trigger : PropertyChanged, INamed
	{
		string name = "Trigger";
		string folder;

		ObservableCollection<TriggerLogic> logic = new ObservableCollection<TriggerLogic>();

		int maxOccurrences = 1;
		bool enabled = true;
		double probability = 1;


		bool isExpanded = true;

		

		[DataMember]
		public double Probability
		{
			get { return probability; }
			set
			{
				if (value > 1 || value < 0) throw new ArgumentException("must be between 0 and 1");
				probability = value;
				RaisePropertyChanged("Probability");
			}
		}

		[DataMember]
		public bool Enabled
		{
			get { return enabled; }
			set
			{
				enabled = value;
				RaisePropertyChanged("Enabled");
			}
		}

		[DataMember]
		public int MaxOccurrences
		{
			get { return maxOccurrences; }
			set
			{
				maxOccurrences = value;
				RaisePropertyChanged("MaxOccurrences");
			}
		}

		[DataMember]
		public ObservableCollection<TriggerLogic> Logic
		{
			get { return logic; }
			set
			{
				logic = value;
				RaisePropertyChanged("Logic");
			}
		}

		[DataMember]
		public string Name
		{
			get { return name; }
			set
			{
				name = value;
				RaisePropertyChanged("Name");
			}
		}

		public IEnumerable<Action> Actions
		{
			get { return logic.OfType<Action>(); }
		}

		public IEnumerable<Condition> Conditions
		{
			get { return logic.AsQueryable().OfType<Condition>(); }
		}

		public override string ToString()
		{
			return name;
		}

		public IEnumerable<UnitStartInfo> AllUnits
		{
			get { return logic.OfType<CreateUnitsAction>().SelectMany(a => a.Units); }
		}

		[DataMember]
		public bool IsExpanded
		{
			get { return isExpanded; } 
			set
			{
				isExpanded = value;
				RaisePropertyChanged("IsExpanded");
			}
		}


		[DataMember]
		public string Folder
		{
			get { return folder; } 
			set
			{
				folder = value;
				RaisePropertyChanged("Folder");
			}
		}

		public LuaTable GetLuaMap(Mission mission)
		{
			var logicMaps = new List<LuaTable>();
			foreach (var item in Logic)
			{
				var itemMap = new Dictionary<object, object>
					{
						{"logicType", item.GetType().Name},
						{"args", item.GetLuaTable(mission)},
						{"name", item.Name},
					};
				logicMaps.Add(new LuaTable(itemMap));
			}
			var map = new Dictionary<object, object>
				{
                    {"name", name},
					{"logic", LuaTable.CreateArray(logicMaps)},
					{"maxOccurrences", MaxOccurrences},
					{"enabled", enabled},
					{"probability", Probability},
				};
			return new LuaTable(map);
		}
	}
}