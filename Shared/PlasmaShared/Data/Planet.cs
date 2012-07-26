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

     
        public Faction GetAttacker(IEnumerable<int> presentFactions) {
            return PlanetFactions.Where(x => presentFactions.Contains(x.FactionID) && x.FactionID != OwnerFactionID && x.Dropships > 0).OrderByDescending(x => x.Dropships).ThenBy(x => x.DropshipsLastAdded).Select(x => x.Faction).FirstOrDefault();
        }


	    public string GetColor(Account viewer)
		{
			if (Account == null || Account.Clan == null) return "#808080";
			else return Account.Clan.Faction.Color;// todo stupid faction way
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
            
            if (Faction != null && (Faction == attacker || Faction.HasTreatyRight(attacker, x => x.EffectPreventDropshipAttack == true, this))) return false; // attacker allied cannot strike

            if (Faction == null && !attacker.Planets.Any()) return true; // attacker has no planets, planet neutral, allow strike


            // iterate links to this planet
            foreach (var link in LinksByPlanetID1.Union(LinksByPlanetID2))
            {
                var otherPlanet = PlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;
                
                // planet has wormhole active
                if (otherPlanet.PlanetStructures.Any(x=>x.IsActive && x.StructureType.EffectAllowShipTraversal == true)) {
                    
                    // planet belongs to attacker or person who gave attacker rights to pass
                    if (otherPlanet.Faction != null && (otherPlanet.Faction == attacker || attacker.HasTreatyRight(otherPlanet.Faction,x=>x.EffectAllowDropshipPass == true, otherPlanet))) {
                        return true;

                    }


                }
            }
            return false;


        }

	}
}