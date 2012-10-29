using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ShakeCameraAction : Action
	{
		double strength = 1;

        public ShakeCameraAction()
			: base()	{}

		[DataMember]
		public double Strength
		{
			get { return strength; }
			set
			{
				strength = value;
				RaisePropertyChanged("Strength");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"strength", Strength},
				};
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Shake Camera";
		}
	}
}