using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class LockUnitsAction : Action
	{
		public LockUnitsAction()
			: base("Lock Units")
		{
			Units = new ObservableCollection<string>();
		}

		[DataMember]
		public ObservableCollection<string> Units { get; set; }

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<string, object>
				{
					{"units", new LuaTable(Units)},
				};
			return new LuaTable(map);
		}
	}
}