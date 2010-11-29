using System.Collections.Generic;
using CMissionLib;

namespace MissionEditor2
{
	public class ConditionsFolder: PropertyChanged
	{
		public Trigger Trigger { get; private set; }

		public IEnumerable<Condition> Items { get { return Trigger.Conditions; } }

		public string Name { get; private set; }

		public ConditionsFolder(Trigger trigger)
		{
			this.Trigger = trigger;
			Name = "Conditions";
			trigger.Logic.CollectionChanged += (s, e) => RaisePropertyChanged("Items");
		}

		public override string ToString()
		{
			return string.Format("{0}: {1}", GetType().Name, Name);
		}
	}
}