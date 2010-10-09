using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class RenameTriggersAction : Action
	{
		string newName = "New Name";
		ObservableCollection<INamed> triggers = new ObservableCollection<INamed>();
		public RenameTriggersAction() : base("Rename Triggers") {}

		public ObservableCollection<INamed> Triggers
		{
			get { return triggers; }
			set
			{
				triggers = value;
				RaisePropertyChanged("Triggers");
			}
		}

		public string NewName
		{
			get { return newName; }
			set
			{
				newName = value;
				RaisePropertyChanged("NewName");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			throw new NotImplementedException();
		}
	}
}