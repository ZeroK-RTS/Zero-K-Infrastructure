using System;

namespace PlasmaShared
{
    public class BattleResult
    {
        public int Duration;
        public string EngineBattleID;
        public DateTime? IngameStartTime;
        public DateTime StartTime;
        public string ReplayName { get; set; }
    }
}