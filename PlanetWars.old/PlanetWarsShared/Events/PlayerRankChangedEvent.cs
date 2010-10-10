using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetWarsShared.Events
{
    [Serializable]
    public class PlayerRankChangedEvent : Event
    {
        public PlayerRankChangedEvent() { }

        public Rank OldRank;
        public Rank NewRank;
        public string PlayerName;

        public PlayerRankChangedEvent(DateTime dateTime, Galaxy galaxy, string name, Rank oldRank, Rank newRank)
            : base(dateTime, galaxy)
        {
            OldRank = oldRank;
            NewRank = newRank;
            if (oldRank == newRank) throw new ArgumentException("Ranks must differ for player rank changed event");
            PlayerName = name;
        }

        public override bool IsFactionRelated(string factionName)
        {
            return Galaxy.GetPlayer(PlayerName).FactionName == factionName;
        }

        public override bool IsPlanetRelated(int planetID)
        {
            return false;
        }

        public override bool IsPlayerRelated(string playerName)
        {
            return playerName == PlayerName;
        }

        public override string ToHtml()
        {
            if (NewRank > OldRank)
            {
                return string.Format(
                    "Congratulations, {0} was promoted from {1} to {2}!",
                    Player.ToHtml(PlayerName),
                    OldRank,
                    NewRank);
            }
              return string.Format(
                    "Sadly, {0} was demoted from {1} to {2}",
                    Player.ToHtml(PlayerName),
                    OldRank,
                    NewRank);


        }
    }
}
    
