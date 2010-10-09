using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib
{
	[DataContract]
	public abstract class TriggerLogic : PropertyChanged
	{
		string name;

		public abstract LuaTable GetLuaTable(Mission mission);

		protected TriggerLogic(string name)
		{
			this.name = name;
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

		public string Type
		{
			get { return GetType().Name; }
		}

		// helps with collectionview property grouping
		public TriggerLogic This
		{
			get { return this; }
		}

		public override string ToString()
		{
			return name;
		}
	}
}
