using System;

namespace PlanetWarsShared
{
    [Serializable]
    public class Award
    {
        public string Type;
        public string Text;
        public DateTime IssuedOn = DateTime.Now;
        public int PlanetID;
        public int Turn;
        public int Round;
        public Award() {}
        public Award(string type, string text, int planetID, int turn, int round)
        {
            Type = type;
            Text = text;
            IssuedOn = DateTime.Now.ToUniversalTime();
            PlanetID = planetID;
            Turn = turn;
            Round = round;
        }
    }
}