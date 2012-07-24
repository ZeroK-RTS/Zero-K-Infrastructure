using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ZkData
{
	partial class Planet
	{
		public const double OverlayRatio = 2.25;

		public IEnumerable<FactionInfluence> GetFactionInfluences()
		{
			return
				AccountPlanets.GroupBy(x => x.Account.Faction).Where(x => x.Key != null).Select(
					x => new FactionInfluence() { Faction = x.Key, Influence = x.Sum(y => (int?)(y.Influence + y.ShadowInfluence)) ?? 0 }).OrderByDescending(
						x => x.Influence);
		}

        public bool TreatyAttackablePlanet( Clan clan)
        {
            var planet = this;
            if (planet != null && clan != null && planet.OwnerAccountID != null) if (planet.Account.FactionID == clan.FactionID || planet.Account.Clan.GetEffectiveTreaty(clan).AllyStatus >= AllyStatus.Ceasefire) return false;
            return true;
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

		public class FactionInfluence
		{
			public Faction Faction;
			public int Influence;
		}

	    public int GetUpkeepCost()
	    {
            return PlanetStructures.Sum(y => (int?)y.StructureType.UpkeepEnergy)??0;
	    }

	    public double GetIPToCapture() {
	        return 50; // todo cleanup
	    }
	}
}