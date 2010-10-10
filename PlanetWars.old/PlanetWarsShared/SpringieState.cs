using System;
using PlanetWarsShared.Springie;

namespace PlanetWarsShared
{
    [Serializable]
    public class SpringieState
    {
        public SpringieState(int planetID, ReminderEvent reminderEvent, string offensiveFactionName)
        {
            PlanetID = planetID;
            ReminderEvent = reminderEvent;
            OffensiveFactionName = offensiveFactionName;
            LastUpdate = DateTime.Now;
        }

        public SpringieState() {}

        public DateTime LastUpdate { get; set; }
        public SpringieState GameStartedStatus { get; set; }
        public int  PlanetID { get; set; }
        public ReminderEvent ReminderEvent { get; set; }
        public string OffensiveFactionName { get; set; }
    }
}