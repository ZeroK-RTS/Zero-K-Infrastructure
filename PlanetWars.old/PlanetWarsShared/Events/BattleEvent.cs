#region using

using System;
using System.Collections.Generic;
using System.Linq;
using PlanetWarsShared.Springie;

#endregion

namespace PlanetWarsShared.Events
{
    [Serializable]
    public class BattleEvent : Event
    {
        public bool AreUpgradesDisabled;
        public string Attacker;
        public string Defender;
        public int PlanetID = -1;
        public string Victor;

        public int[] EncircledPlanets;
        public string[] SpaceFleetOwners;

        BattleEvent() {}

        public BattleEvent(DateTime dateTime,
                           List<EndGamePlayerInfo> endGameInfos,
                           string mapName,
                           string attacker,
                           string defender,
                           string victor,
                           bool areUpgradesDisabled,
                           Galaxy galaxy,
                           IEnumerable<int> encircledPlanets,
                           IEnumerable<string> spaceFleetOwners) : base(dateTime, galaxy)
        {
            EndGameInfos = endGameInfos;
            MapName = mapName;
            PlanetID = Galaxy.Planets.Single(p => p.MapName == MapName).ID;
            Attacker = attacker;
            Defender = defender;
            AreUpgradesDisabled = areUpgradesDisabled;
            Victor = victor;
            EncircledPlanets = encircledPlanets.ToArray();
            SpaceFleetOwners = spaceFleetOwners.ToArray();
        }

        public List<EndGamePlayerInfo> EndGameInfos { get; set; }
        public string MapName { get; set; }

        public override bool IsPlayerRelated(string playerName)
        {
            return EndGameInfos.Any(p => p.Name == playerName);
        }

        public override bool IsPlanetRelated(int planetID)
        {
            return Galaxy.GetPlanet(planetID).MapName == MapName;
        }

        public override bool IsFactionRelated(string factionName)
        {
            return EndGameInfos.Any(p => Galaxy.GetPlayer(p.Name).FactionName == factionName);
        }

        public override string ToHtml()
        {
            var planet = Galaxy.GetPlanet(PlanetID);
            var aWinningPlayer = EndGameInfos.FirstOrDefault(p => p.OnVictoryTeam);

            if (aWinningPlayer == null) {
                return string.Format(
                    "The fight for {0} ended in a draw. <a href='battle.aspx?turn={1}'>Details.</a>", planet, Turn);
            }
            var aLosingPlayer = EndGameInfos.FirstOrDefault(p => !p.OnVictoryTeam);
            if (aLosingPlayer == null) {
                return string.Format(
                    "The fight for {0} ended in a draw. <a href='battle.aspx?turn={1}'>Details.</a>", planet, Turn);
            }

            string format = "";
            format = Attacker == Victor ? "{0} captured {1} from {2}" : "{2} stopped attack on {1} from {0}";
            format += ". <a href='battle.aspx?turn={3}'>Details.</a>";

            return string.Format(
                format, Faction.ToHtml(Attacker), Planet.ToHtml(planet.Name, planet.ID), Faction.ToHtml(Defender), Turn);
        }
    }
}