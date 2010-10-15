using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class CancelCountdownAction : Action
	{
		string countdown;

		public CancelCountdownAction(string countdown)
			: base("Cancel Countdown")
		{
			this.Countdown = countdown;
		}

		[DataMember]
		public string Countdown
		{
			get { return countdown; }
			set
			{
				countdown = value;
				RaisePropertyChanged("Countdown");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"countdown", Countdown??string.Empty},
				};
			return new LuaTable(map);
		}
	}
}