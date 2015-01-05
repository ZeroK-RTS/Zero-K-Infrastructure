using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ZkData
{
    public class Galaxy
    {
        
        public Galaxy()
        {
            Links = new HashSet<Link>();
            Planets = new HashSet<Planet>();
        }

        public int GalaxyID { get; set; }
        public DateTime? Started { get; set; }
        public int Turn { get; set; }
        public DateTime? TurnStarted { get; set; }
        [StringLength(100)]
        public string ImageName { get; set; }
        public bool IsDirty { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsDefault { get; set; }
        public int AttackerSideCounter { get; set; }
        public DateTime? AttackerSideChangeTime { get; set; }
        [Column(TypeName = "text")]
        public string MatchMakerState { get; set; }
        
        public virtual ICollection<Link> Links { get; set; }
        public virtual ICollection<Planet> Planets { get; set; }


        /// <summary>
        /// Decay influence where planet has multiple faction owners
        /// </summary>
        public void DecayInfluence()
        {
            foreach (var planet in Planets.Where(x => x.PlanetFactions.Count(y => y.Influence > 0) > 1))
            {
                foreach (var pf in planet.PlanetFactions.Where(x => x.Influence > 0))
                {
                    pf.Influence = Math.Max((double)0, pf.Influence - GlobalConst.InfluenceDecay);
                }
            }
        }


        public void ProcessProduction()
        {
            foreach (var grp in Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive && x.Account != null).GroupBy(x => x.Account))
            {
                var structs = grp.ToList();
                var drops = structs.Where(x => x.StructureType.EffectDropshipProduction > 0).Sum(x => x.StructureType.EffectDropshipProduction) ?? 0;
                grp.Key.ProduceDropships(drops);

                var bombers = structs.Where(x => x.StructureType.EffectBomberProduction > 0).Sum(x => x.StructureType.EffectBomberProduction) ?? 0;
                grp.Key.ProduceBombers(bombers);


                var warps = structs.Where(x => x.StructureType.EffectWarpProduction > 0).Sum(x => x.StructureType.EffectWarpProduction) ?? 0;
                grp.Key.ProduceWarps(warps);
            }

            // planets generate metal
            foreach (var p in Planets.Where(x => x.Faction != null && x.Account != null))
            {
                p.Account.ProduceMetal(GlobalConst.PlanetMetalPerTurn);
            }
        }



        /// <summary>
        /// Spread influence through wormholes
        /// </summary>
        public void SpreadInfluence()
        {


            foreach (var planet in Planets)
            {
                var sumInfluence = planet.PlanetFactions.Sum(x => (double?)x.Influence) ?? 0;
                var freeRoom = 100 - sumInfluence;
                if (freeRoom > 0)
                {
                    var spreads = new Dictionary<Faction, double>();

                    var hasInhibitor = planet.PlanetStructures.Any(x => x.IsActive && x.StructureType.EffectBlocksInfluenceSpread == true);

                    // check all linked planets to spread influence here
                    foreach (var link in planet.LinksByPlanetID1.Union(planet.LinksByPlanetID2))
                    {
                        var otherPlanet = link.PlanetID1 == planet.PlanetID ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;

                        if (otherPlanet.Faction != null)
                        {
                            if (otherPlanet.Faction != planet.Faction && hasInhibitor) continue;

                            // diplomacy check
                            if (!otherPlanet.Faction.GaveTreatyRight(planet, x => x.EffectPreventInfluenceSpread == true))
                            {
                                var spread =
                                    otherPlanet.PlanetStructures.Where(x => x.IsActive).Sum(x => (double?)(x.StructureType.EffectInfluenceSpread ?? 0)) ??
                                    0;

                                double oldVal;
                                spreads.TryGetValue(otherPlanet.Faction, out oldVal);
                                spreads[otherPlanet.Faction] = oldVal + spread;
                            }

                        }
                    }

                    // handle guerilla jumpgates
                    foreach (var guerillaWormhole in planet.PlanetStructuresByTargetPlanetID.Where(x => x.IsActive && x.StructureType.EffectRemoteInfluenceSpread > 0))
                    {
                        var sourcePlanet = guerillaWormhole.Planet;

                        if (sourcePlanet.Faction != null)
                        {
                            if (sourcePlanet.Faction != planet.Faction && hasInhibitor) continue;

                            // diplomacy check
                            if (!sourcePlanet.Faction.GaveTreatyRight(planet, x => x.EffectPreventInfluenceSpread == true))
                            {
                                var spread = guerillaWormhole.StructureType.EffectRemoteInfluenceSpread ?? 0;

                                double oldVal;
                                spreads.TryGetValue(sourcePlanet.Faction, out oldVal);
                                spreads[sourcePlanet.Faction] = oldVal + spread;
                            }

                        }
                    }


                    // same-planet spread
                    if (planet.Faction != null)
                    {
                        var autospread = planet.PlanetStructures.Where(x => x.IsActive).Sum(x => (double?)(x.StructureType.EffectInfluenceSpread ?? 0)) ?? 0;
                        double oldVal2;
                        spreads.TryGetValue(planet.Faction, out oldVal2);
                        spreads[planet.Faction] = oldVal2 + autospread;
                    }

                    if (spreads.Count > 0)
                    {
                        var sumSpreads = spreads.Sum(x => x.Value);
                        double squeeze = 1.0;
                        if (sumSpreads > freeRoom) squeeze = freeRoom / sumSpreads;

                        foreach (var kvp in spreads)
                        {
                            var entry = planet.PlanetFactions.SingleOrDefault(x => x.FactionID == kvp.Key.FactionID);
                            if (entry == null)
                            {
                                entry = new PlanetFaction() { FactionID = kvp.Key.FactionID, PlanetID = planet.PlanetID };
                                planet.PlanetFactions.Add(entry);
                            }
                            var gain = kvp.Value * squeeze;

                            if (kvp.Key != planet.Faction && entry.Influence < GlobalConst.InfluenceToCapturePlanet && entry.Influence + gain >= GlobalConst.InfluenceToCapturePlanet)
                            {
                                entry.Influence = GlobalConst.InfluenceToCapturePlanet - 0.1;
                            }
                            else entry.Influence += gain;

                            if (entry.Influence > 100) entry.Influence = 100;

                        }
                    }



                }
            }

        }
    }
}
