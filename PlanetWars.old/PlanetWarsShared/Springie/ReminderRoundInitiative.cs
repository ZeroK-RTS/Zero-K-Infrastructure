using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetWarsShared.Springie {
	[Flags, Serializable]
	public enum ReminderRoundInitiative {
		Defense = 1,
		Offense = 2,
	}
}
