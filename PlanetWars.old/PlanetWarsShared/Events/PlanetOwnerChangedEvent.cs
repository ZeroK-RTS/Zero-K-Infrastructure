using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetWarsShared.Events
{
    [Serializable]
    public class PlanetOwnerChangedEvent:Event
    {
        public PlanetOwnerChangedEvent() {}
        public PlanetOwnerChangedEvent(DateTime dateTime, Galaxy galaxy, string fromName, string toName, string faction, int planetID) : base(dateTime, galaxy)
        {
            FromName = fromName;
            ToName = toName;
            Faction = faction;
            PlanetID = planetID;
        }

        public string FromName;
        public string ToName;
        public string Faction;
        public int PlanetID;

        public override bool IsFactionRelated(string factionName)
        {
            return factionName == Faction;
        }

        public override bool IsPlanetRelated(int planetID)
        {
            return PlanetID == planetID;
        }

        public override bool IsPlayerRelated(string playerName)
        {
            return playerName == FromName || playerName == ToName;
        }

        public override string ToHtml()
        {
            return
                string.Format(
                    "{0} command has transferred ownership of planet {1} from {2} to more capable {3}",
                    Faction,
                    Planet.ToHtml(Galaxy.GetPlanet(PlanetID).Name, PlanetID),
                    Player.ToHtml(FromName),
                    Player.ToHtml(ToName));
        }
    }
}
