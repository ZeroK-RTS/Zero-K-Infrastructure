using System;
using System.Collections.Generic;
using System.Linq;

namespace ZkData
{
    partial class Galaxy
    {
        /// <summary>
        /// Decay influence where planet has multiple faction owners
        /// </summary>
        public void DecayInfluence() {
            foreach (var planet in Planets.Where(x=>x.PlanetFactions.Count(y=>y.Influence > 0) > 1)) {
                foreach (var pf in planet.PlanetFactions.Where(x=>x.Influence > 0)) {
                    pf.Influence = Math.Min(0, pf.Influence - GlobalConst.InfluenceDecay);
                }
            }
        }


        /// <summary>
        /// Spread influence through wormholes
        /// </summary>
        public void SpreadInfluence() {

            foreach (var planet in Planets) {
                var sumInfluence = planet.PlanetFactions.Sum(x => (double?)x.Influence) ?? 0;
                var freeRoom = 100 - sumInfluence;
                if (freeRoom > 0) {
                    var spreads = new Dictionary<Faction, double>();

                    var hasInhibitor = planet.PlanetStructures.Any(x => x.IsActive && x.StructureType.EffectBlocksInfluenceSpread == true);

                    // check all linked planets to spread influence here
                    foreach (var link in planet.LinksByPlanetID1.Union(planet.LinksByPlanetID2)) {
                        var otherPlanet = link.PlanetID1 == planet.PlanetID ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;
                        
                        if (otherPlanet.Faction != null) {
                            if (otherPlanet.Faction != planet.Faction && hasInhibitor) continue;

                            // diplomacy check
                            if (planet.Faction == null || !planet.Faction.HasTreatyRight(otherPlanet.Faction, x => x.EffectPreventInfluenceSpread == true, planet)) {


                                var spread =
                                    otherPlanet.PlanetStructures.Where(x => x.IsActive).Sum(x => (double?)(x.StructureType.EffectInfluenceSpread ?? 0)) ??
                                    0;

                                double oldVal;
                                spreads.TryGetValue(otherPlanet.Faction, out oldVal);
                                spreads[otherPlanet.Faction] = oldVal + spread;
                            }

                        }
                    }

                    if (spreads.Count > 0) {
                        var sumSpreads = spreads.Sum(x => x.Value);
                        double squeeze = 1.0;
                        if (sumSpreads > freeRoom) squeeze = freeRoom/sumSpreads;
                        
                        foreach (var kvp in spreads) {
                            var entry = planet.PlanetFactions.SingleOrDefault(x => x.FactionID == kvp.Key.FactionID);
                            if (entry == null) {
                                entry = new PlanetFaction(){FactionID = kvp.Key.FactionID, PlanetID = planet.PlanetID};
                                planet.PlanetFactions.Add(entry);
                            }
                            entry.Influence += kvp.Value*squeeze;
                        }
                    }



                }
            }

        }



    }
}