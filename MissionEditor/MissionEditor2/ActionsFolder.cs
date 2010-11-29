using System.Collections.Generic;
using System.Collections.ObjectModel;
using CMissionLib;

namespace MissionEditor2
{
	public class ActionsFolder: PropertyChanged
	{
		public Trigger Trigger { get; private set; }

		public IEnumerable<Action> Items
		{
			get { return Trigger.Actions; }
		}

		public string Name { get; private set; }

		public ActionsFolder(Trigger trigger)
		{
			this.Trigger = trigger;
			Name = "Actions";
			trigger.Logic.CollectionChanged += (s, e) => RaisePropertyChanged("Items");
		}

		public override string ToString()
		{
			return string.Format("{0}: {1}", GetType().Name, Name);
		}
	}
}