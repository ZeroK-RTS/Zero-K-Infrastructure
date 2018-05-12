using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Remoting.Channels;
using ZkData.Migrations;

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

        [StringLength(100)]
        public string ImageName { get; set; }
        public bool IsDirty { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsDefault { get; set; }
        public int AttackerSideCounter { get; set; }
        public DateTime? AttackerSideChangeTime { get; set; }
        
        public string MatchMakerState { get; set; }

        public DateTime? Ended { get; set; }

        public string EndMessage { get; set; }
        
        public virtual ICollection<Link> Links { get; set; }
        public virtual ICollection<Planet> Planets { get; set; }

        public virtual Faction WinnerFaction { get; set; }

        [ForeignKey(nameof(WinnerFaction))]
        public int? WinnerFactionID { get; set; }


        /// <summary>
        /// Decay influence where planet has multiple faction owners
        /// </summary>
        public void DecayInfluence()
        {
            foreach (var planet in Planets.Where(x => x.PlanetFactions.Count(y => y.Influence > 0) > 1))
            {
                var influenceDecayMin = planet.PlanetStructures.Where(x => x.IsActive && x.StructureType.EffectPreventInfluenceDecayBelow != null).Select(x=>x.StructureType.EffectPreventInfluenceDecayBelow).OrderByDescending(x => x).FirstOrDefault();
                
                foreach (var pf in planet.PlanetFactions.Where(x => x.Influence > 0))
                {
                    pf.Influence = Math.Max((double)0, pf.Influence - GlobalConst.InfluenceDecay);

                    if (pf.FactionID == planet.OwnerFactionID && influenceDecayMin > 0) pf.Influence = Math.Max(pf.Influence, influenceDecayMin.Value);
                }
                
            }
        }


        public void ProcessProduction()
        {
            foreach (var grp in Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive && x.Account != null && x.Account.Faction != null).GroupBy(x => x.Account))
            {
                var structs = grp.ToList();
                var drops = structs.Where(x => x.StructureType.EffectDropshipProduction > 0).Sum(x => x.StructureType.EffectDropshipProduction) ?? 0;
                grp.Key.ProduceDropships(drops);

                var bombers = structs.Where(x => x.StructureType.EffectBomberProduction > 0).Sum(x => x.StructureType.EffectBomberProduction) ?? 0;
                grp.Key.ProduceBombers(bombers);
                
                var warps = structs.Where(x => x.StructureType.EffectWarpProduction > 0).Sum(x => x.StructureType.EffectWarpProduction) ?? 0;
                grp.Key.ProduceWarps(warps);
            }
            // structures without owning individual (or individual is somehow not in a faction)
            foreach (var grp in Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive && (x.Account == null || x.Account.Faction == null)).GroupBy(x => x.Planet.Faction))
            {
                var structs = grp.ToList();
                var drops = structs.Where(x => x.StructureType.EffectDropshipProduction > 0).Sum(x => x.StructureType.EffectDropshipProduction) ?? 0;
                grp.Key.ProduceDropships(drops);

                var bombers = structs.Where(x => x.StructureType.EffectBomberProduction > 0).Sum(x => x.StructureType.EffectBomberProduction) ?? 0;
                grp.Key.ProduceBombers(bombers);

                var warps = structs.Where(x => x.StructureType.EffectWarpProduction > 0).Sum(x => x.StructureType.EffectWarpProduction) ?? 0;
                grp.Key.ProduceWarps(warps);
            }

            // produce victory points
            foreach (var fac in Planets.Where(x => x.Faction != null).GroupBy(x => x.Faction))
            {
                fac.Key.VictoryPoints +=
                    fac.SelectMany(x => x.PlanetStructures)
                        .Where(x => x.IsActive && x.StructureType.EffectVictoryPointProduction != null)
                        .Sum(x => x.StructureType.EffectVictoryPointProduction) ?? 0;
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
                var freeRoom = GlobalConst.PlanetWarsMaximumIP - sumInfluence;
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


                    /*
                     * Replacement proposal for free = 0
                     * var sumSpreads = spreads.Sum(x => x.Value);
double squeeze = 1.0;
if (sumSpreads > 100) 
  squeeze = 100 / sumSpreads
  foreach spread IP
    IP *= squeeze
  end for
  sumSpreads = 100
end if

if (sumSpreads > freeRoom)
  factor = (100 - sumSpreads)/(100 - freeRoom)
  foreach current IP
    IP *= factor
  end for
end if
that is it
                     */

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

                            /*if (kvp.Key != planet.Faction && entry.Influence < GlobalConst.InfluenceToCapturePlanet && entry.Influence + gain >= GlobalConst.InfluenceToCapturePlanet)
                            {
                                entry.Influence = GlobalConst.InfluenceToCapturePlanet - 0.1;
                            }
                            else */
                            entry.Influence += gain;

                            if (entry.Influence > GlobalConst.PlanetWarsMaximumIP) entry.Influence = GlobalConst.PlanetWarsMaximumIP;

                        }
                    }



                }
            }

        }

        public void DeleteOneTimeActivated(IPlanetwarsEventCreator eventCreator, ZkDataContext db)
        {
            var todel = new List<PlanetStructure>();
            foreach (var structure in Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive && x.StructureType.IsSingleUse == true && !x.StructureType.RequiresPlanetTarget))
            {
                todel.Add(structure);
                db.Events.Add(eventCreator.CreateEvent("{0}'s {1} on planet {2} has activated and is now removed",
                    structure.Account,
                    structure.StructureType,
                    structure.Planet));
            }

            db.PlanetStructures.DeleteAllOnSubmit(todel);
        }

    }
}
