using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ZkData
{
	partial class Planet
	{
		public const double OverlayRatio = 2.25;

		public IEnumerable<PlanetFaction> GetFactionInfluences()
		{
			return PlanetFactions.Where(x=>x.Influence > 0).OrderByDescending(x => x.Influence);
		}


	    public override string ToString() {
	        return Name;
	    }

	    public string GetColor(Account viewer)
		{
            if (OwnerFactionID == null) return "#A0A0A0";
            else return Faction.Color;
		}


		public Rectangle PlanetOverlayRectangle(Galaxy gal)
		{
			var w = Resource.PlanetWarsIconSize*OverlayRatio;
			var xp = (int)(X*gal.Width);
			var yp = (int)(Y*gal.Height);
			return new Rectangle((int)(xp - w/2), (int)(yp - w/2), (int)w, (int)w);
		}

		public Rectangle PlanetRectangle(Galaxy gal)
		{
			var w = Resource.PlanetWarsIconSize;
			var xp = (int)(X*gal.Width);
			var yp = (int)(Y*gal.Height);
			return new Rectangle((int)(xp - w/2), (int)(yp - w/2), (int)w, (int)w);
		}

	    public int GetUpkeepCost()
	    {
            return PlanetStructures.Sum(y => (int?)y.StructureType.UpkeepEnergy)??0;
	    }

        public bool CanDropshipsAttack(Faction attacker) {
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
	        if (CanDropshipsAttack(attacker) ||
	            PlanetFactions.Where(x => x.FactionID == attacker.FactionID).Sum(y => y.Dropships) >
	            PlanetStructures.Where(x => x.IsActive).Sum(y => y.StructureType.EffectDropshipDefense)) return true;
	        else return false;
	    }

        public bool CanFirePlanetBuster(Faction attacker)
        {
            if (!Galaxy.IsDefault) return false;    // no exo-galaxy strikes
            if (OwnerFactionID == attacker.FactionID || attacker.GaveTreatyRight(this, x => x.EffectPreventBomberAttack == true)) return false; // attacker allied cannot strike
            //if (PlanetStructures.Any(x => x.StructureType.EffectIsVictoryPlanet == true)) return false; // artefact protects planet
            return true;
        }


        public bool CheckLinkAttack(Faction attacker, Func<TreatyEffectType, bool> preventingTreaty, Func<TreatyEffectType, bool> passageTreaty) {

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


        public bool CheckWarpAttack(Faction attacker, Func<TreatyEffectType, bool> preventingTreaty)
        {
            if (!Galaxy.IsDefault) return false;    // no exo-galaxy strikes
            if (OwnerFactionID == attacker.FactionID || attacker.GaveTreatyRight(this, preventingTreaty)) return false; // attacker allied cannot strike
            if (PlanetStructures.Any(x => x.IsActive && x.StructureType.EffectBlocksJumpgate == true)) return false; // inhibitor active
            
            return true;
        }

	}
}