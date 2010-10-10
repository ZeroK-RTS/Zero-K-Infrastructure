using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceData
{
	partial class Config
	{
		[DataMember]
		public int GameSecond
		{
			get
			{
				var s = TurnStarted ?? Started;
				if (s != null) return (int)DateTime.UtcNow.Subtract(s.Value).TotalSeconds + CombatTurn * SecondsPerTurn;
				else return 0;
			}
			set { throw new NotSupportedException();}
		}


		public DateTime GetTurnTime(int turn)
		{
			var s = TurnStarted ?? Started;
			if (s == null) throw new ApplicationException("Game not started yet");
			return s.Value.AddSeconds((turn - CombatTurn)*SecondsPerTurn);
		}

	}
}
