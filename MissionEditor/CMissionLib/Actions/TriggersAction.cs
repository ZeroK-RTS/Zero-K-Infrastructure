using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public abstract class TriggersAction : Action
	{
		ObservableCollection<INamed> triggers = new ObservableCollection<INamed>();

		protected TriggersAction(string name) : base(name) {}

		[DataMember]
		public ObservableCollection<INamed> Triggers
		{
			get { return triggers; }
			set
			{
				triggers = value;
				RaisePropertyChanged("Triggers");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{

			foreach (var trigger in Triggers.ToArray())
			{
				if (!mission.Triggers.Contains(trigger))
				{
					Triggers.Remove(trigger);
				}
			}
			var triggerArray = triggers.Select(t => mission.Triggers.IndexOf((Trigger) t) + 1).Cast<object>().ToArray();
			var map = new Dictionary<object, object>
				{
					{"triggers", LuaTable.CreateArray(triggerArray)},
				};
			return new LuaTable(map);
		}
	}
}