using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetWarsShared.Springie {
	[Flags, Serializable]
	public enum ReminderEvent {
		None = 0,
		OnBattlePreparing = 1,
		OnBattleStarted = 2,
		OnBattleEnded = 4,
	}
}
