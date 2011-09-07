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


        public int GetMineIncome() {
            return PlanetStructures.Where(y => !y.IsDestroyed).Sum(y => y.StructureType.EffectCreditsPerTurn) ?? 0;
        }

        public int GetTaxIncome() {
            if (OwnerAccountID != null)
                return (AccountPlanets.Where(y => y.Account.FactionID == Account.FactionID).Sum(y => (int?)y.ShadowInfluence + (int?)y.Influence) ?? 0)/50;
            else return 0;
        }

        public double GetCorruption() {
            var influences = GetFactionInfluences().Select(x => (int?)x.Influence);
            return (influences.Skip(1).FirstOrDefault() ?? 0) / (double)(influences.FirstOrDefault() ?? 1);        
        }


	    public string GetColor(Account viewer)
		{
			if (Account == null || Account.Clan == null) return "#808080";
			else return Account.Clan.Faction.Color;// todo stupid faction way
		}

		public int GetIPToCapture()
		{
			var ownerIP = 0;
			if (Account != null && Account.ClanID != null) ownerIP = AccountPlanets.Where(x => x.Account.FactionID == Account.FactionID).Sum(x => (int?)(x.Influence + x.ShadowInfluence)) ?? 0;
			ownerIP += PlanetStructures.Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectInfluenceDefense) ?? 0;

			return ownerIP;
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
	}
}