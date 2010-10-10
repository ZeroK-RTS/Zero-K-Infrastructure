using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceData
{
	partial class Event1
	{
		
		public void Connect(params Player[] players)
		{
			if (players != null)
			{
				foreach (var player in players)
				{
					if (player != null) PlayerEvents.Add(new PlayerEvent() { Player = player });
				}
			}
		}

		public void Connect(params Battle[] battles)
		{
			if (battles != null)
			{
				foreach (var battle in battles)
				{
					if (battle != null) BattleEvents.Add(new BattleEvent() { Battle = battle });
				}
			}
		}

		public void Connect(params CelestialObject[] objects)
		{
			if (objects != null)
			{
				foreach (var o in objects)
				{
					if (o != null) CelestialObjectEvents.Add(new CelestialObjectEvent() { CelestialObject = o });
				}
			}
		}

		public void Connect(params Fleet[] fleets)
		{
			if (fleets != null)
			{
				foreach (var f in fleets)
				{
					if (f != null) FleetEvents.Add(new FleetEvent() { Fleet = f });
				}
			}
		}


		public static Event1 CreateEvent(EventType type, int battleTurn, string text, params object[] args)
		{
			var ev = new Event1() { EventType = (int)type, BattleTurn = battleTurn, Text = string.Format(text, args), Time = DateTime.UtcNow };
			return ev;
		}
	}

	public enum EventType
	{
		[DataMember]
		Misc = 0,
		[DataMember]
		Player =1 ,
		[DataMember]
		Battle =2,
		[DataMember]
		Trade =3,
		[DataMember]
		Fleet = 4

	}
}
