namespace PlasmaShared
{
    public class BattlePlayerResult
    {
        public int AllyNumber;
        public bool IsIngameReady;
        public bool IsSpectator;
        public bool IsVictoryTeam;
        public int? LoseTime;
        public string Name { get; private set; }

        public bool IsIngame;

        public BattlePlayerResult(string name)
        {
            Name = name;
        }
    }
}