namespace PlasmaShared
{
    public class BotTeam
    {
        public int AllyID;
        public string BotAI;
        public string BotName;
        public string Owner;

        public bool IsChicken
        {
            get
            {
                return BotAI.ToLower().Contains("chicken"); //meh
            }
        }
    }


}