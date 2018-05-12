using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;

namespace ZkData
{
    public class Planet
    {

        public Planet()
        {
            AccountPlanets = new HashSet<AccountPlanet>();
            LinksByPlanetID1 = new HashSet<Link>();
            LinksByPlanetID2 = new HashSet<Link>();
            PlanetFactions = new HashSet<PlanetFaction>();
            PlanetOwnerHistories = new HashSet<PlanetOwnerHistory>();
            PlanetStructures = new HashSet<PlanetStructure>();
            PlanetStructuresByTargetPlanetID = new HashSet<PlanetStructure>();
            TreatyEffects = new HashSet<TreatyEffect>();
            Events = new HashSet<Event>();
        }

        public int PlanetID { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int? MapResourceID { get; set; }
        public int? OwnerAccountID { get; set; }
        public int GalaxyID { get; set; }
        public int? ForumThreadID { get; set; }
        public int? OwnerFactionID { get; set; }
        public int TeamSize { get; set; }

        public virtual Account Account { get; set; }
        public virtual ICollection<AccountPlanet> AccountPlanets { get; set; }
        public virtual Faction Faction { get; set; }
        public virtual ForumThread ForumThread { get; set; }
        public virtual Galaxy Galaxy { get; set; }
        public virtual ICollection<Link> LinksByPlanetID1 { get; set; }
        public virtual ICollection<Link> LinksByPlanetID2 { get; set; }
        public virtual Resource Resource { get; set; }
        public virtual ICollection<PlanetFaction> PlanetFactions { get; set; }
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }
        public virtual ICollection<PlanetStructure> PlanetStructures { get; set; } = new List<PlanetStructure>();
        public virtual ICollection<PlanetStructure> PlanetStructuresByTargetPlanetID { get; set; }
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; }
        public virtual ICollection<Event> Events { get; set; }


        public const double OverlayRatio = 2.25;

        public IEnumerable<PlanetFaction> GetFactionInfluences()
        {
            return PlanetFactions.Where(x => x.Influence > 0).OrderByDescending(x => x.Influence);
        }


        public override string ToString()
        {
            return Name;
        }

        public string GetColor(Account viewer)
        {
            if (OwnerFactionID == null) return "#A0A0A0";
            else return Faction.Color;
        }


        public Rectangle PlanetOverlayRectangle(Galaxy gal)
        {
            var w = Resource.PlanetWarsIconSize * OverlayRatio;
            var xp = (int)(X * gal.Width);
            var yp = (int)(Y * gal.Height);
            return new Rectangle((int)(xp - w / 2), (int)(yp - w / 2), (int)w, (int)w);
        }

        public Rectangle PlanetRectangle(Galaxy gal)
        {
            var w = Resource.PlanetWarsIconSize;
            var xp = (int)(X * gal.Width);
            var yp = (int)(Y * gal.Height);
            return new Rectangle((int)(xp - w / 2), (int)(yp - w / 2), (int)w, (int)w);
        }

        public int GetUpkeepCost()
        {
            return PlanetStructures.Sum(y => (int?)y.StructureType.UpkeepEnergy) ?? 0;
        }

        public bool CanDropshipsAttack(Faction attacker)
        {
            if (attacker.FactionID == OwnerFactionID) return false; // cannot attack own
            return CheckLinkAttack(attacker, x => x.EffectPreventDropshipAttack == true, x => x.EffectAllowDropshipPass == true);
        }

        public bool CanDropshipsWarp(Faction attacker)
        {
            if (attacker.FactionID == OwnerFactionID) return false; // cannot attack own
            return CheckWarpAttack(attacker, x => x.EffectPreventDropshipAttack == true);
        }

        public bool CanBombersAttack(Faction attacker)
        {
            return CheckLinkAttack(attacker, x => x.EffectPreventBomberAttack == true, x => x.EffectAllowBomberPass == true);
        }

        public bool CanBombersWarp(Faction attacker)
        {
            return CheckWarpAttack(attacker, x => x.EffectPreventBomberAttack == true);
        }

        public bool CanMatchMakerPlay(Faction attacker)
        {
            if (attacker == null) return false;

            if (CanDropshipsAttack(attacker) ||
                PlanetFactions.Where(x => x.FactionID == attacker.FactionID).Sum(y => y.Dropships) >
                PlanetStructures.Where(x => x.IsActive).Sum(y => y.StructureType.EffectDropshipDefense)) return true;
            else return false;
        }

        public bool CanFirePlanetBuster(Faction attacker)
        {
            if (!Galaxy.IsDefault) return false;    // no exo-galaxy strikes
            if (OwnerFactionID == attacker.FactionID || attacker.GaveTreatyRight(this, x => x.EffectPreventBomberAttack == true)) return false; // attacker allied cannot strike
            if (PlanetStructures.Any(x => x.StructureType.EffectIsVictoryPlanet == true || x.StructureType.OwnerChangeWinsGame)) return false; // artefact protects planet
            return true;
        }


        public bool CheckLinkAttack(Faction attacker, Func<TreatyEffectType, bool> preventingTreaty, Func<TreatyEffectType, bool> passageTreaty)
        {

            if (attacker.GaveTreatyRight(this, preventingTreaty)) return false; // attacker allied cannot strike

            if (OwnerFactionID == attacker.FactionID)   // bomb own planet - only allow if other factions have IP
            {
                var otherFactions = PlanetFactions.Count(x => x.FactionID != attacker.FactionID && x.Influence > 0);
                return (otherFactions > 0);
            }

            if (Faction == null && !attacker.Planets.Any()) return true; // attacker has no planets, planet neutral, allow strike


            // iterate links to this planet
            foreach (var link in LinksByPlanetID1.Union(LinksByPlanetID2))
            {
                var otherPlanet = PlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;

                // planet has wormhole active
                if (!GlobalConst.RequireWormholeToTravel || otherPlanet.PlanetStructures.Any(x => x.IsActive && x.StructureType.EffectAllowShipTraversal == true))
                {

                    // planet belongs to attacker or person who gave attacker rights to pass + if planet's faction has blocking treaty with attacker dont allow attack
                    if (otherPlanet.Faction != null && (otherPlanet.OwnerFactionID == attacker.FactionID || attacker.HasTreatyRight(otherPlanet.Faction, passageTreaty, otherPlanet)) && !otherPlanet.Faction.GaveTreatyRight(this, preventingTreaty))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public int? GetLinkDistanceTo(Planet targetPlanet)
        {
            if (this == targetPlanet) return 0;

            int? distance = 0;
            List<Planet> checkedPlanets = new List<Planet>();
            List<Planet> front = new List<Planet>() { this };

            do
            {
                checkedPlanets.AddRange(front);
                List<Planet> newFront = new List<Planet>();

                foreach (var p in front)
                {
                    // iterate links to this planet
                    foreach (var link in p.LinksByPlanetID1.Union(p.LinksByPlanetID2))
                    {
                        var otherPlanet = p.PlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;


                        if (!checkedPlanets.Contains(otherPlanet))
                        {
                            // planet has wormhole active and is traversable

                            if (otherPlanet == targetPlanet) return distance + 1;
                            newFront.Add(otherPlanet);
                        }
                    }
                }
                front = newFront;
                distance++;
            } while (front.Count > 0);

            return null;
        }

        public int? GetLinkDistanceTo(Func<Planet, bool> planetCondition, Faction traverseFaction, out Planet matchPlanet)
        {
            // check if this planet is the condition

            if (planetCondition(this) && (traverseFaction==null|| OwnerFactionID == traverseFaction.FactionID))
            {
                matchPlanet = this;
                return 0;
            }

            int? distance = 0;
            List<Planet> checkedPlanets = new List<Planet>();
            List<Planet> front = new List<Planet>() { this };

            do
            {
                checkedPlanets.AddRange(front);
                List<Planet> newFront = new List<Planet>();

                foreach (var p in front)
                {
                    // iterate links to this planet
                    foreach (var link in p.LinksByPlanetID1.Union(p.LinksByPlanetID2))
                    {
                        var otherPlanet = p.PlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;


                        if (!checkedPlanets.Contains(otherPlanet))
                        {
                            // planet has wormhole active and is traversable
                            if (traverseFaction == null || (otherPlanet.OwnerFactionID == traverseFaction.FactionID &&
                                otherPlanet.PlanetStructures.Any(x => x.IsActive && x.StructureType.EffectAllowShipTraversal == true)))
                            {

                                if (planetCondition(otherPlanet))
                                {
                                    matchPlanet = otherPlanet;
                                    return distance + 1;
                                }
                                newFront.Add(otherPlanet);
                            }
                        }
                    }
                }

                front = newFront;
                distance++;
            } while (front.Count > 0);

            matchPlanet = null;
            return null;
        }

        public double GetEffectiveShipIpBonus(Faction attacker)
        {
            double planetDropshipDefs = (PlanetStructures.Where(x => x.IsActive).Sum(x => x.StructureType.EffectDropshipDefense) ?? 0);
            int dropshipsSent = (PlanetFactions.Where(x => x.Faction == attacker).Sum(x => (int?)x.Dropships) ?? 0);
            return Math.Max(0, (dropshipsSent - planetDropshipDefs)) * GlobalConst.InfluencePerShip;
        }

        public double GetEffectiveIpDefense()
        {
            return (PlanetStructures.Where(x => x.IsActive).Sum(x => x.StructureType.EffectReduceBattleInfluenceGain) ?? 0);
        }


        public bool CheckWarpAttack(Faction attacker, Func<TreatyEffectType, bool> preventingTreaty)
        {
            if (!Galaxy.IsDefault) return false;    // no exo-galaxy strikes
            if (OwnerFactionID == attacker.FactionID || attacker.GaveTreatyRight(this, preventingTreaty)) return false; // attacker allied cannot strike
            if (PlanetStructures.Any(x => x.IsActive && x.StructureType.EffectBlocksJumpgate == true)) return false; // inhibitor active

            return true;
        }

    }
}
