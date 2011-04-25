using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace ZkData
{
	partial class Galaxy
	{
		public static List<Unlock> ClanUnlocks(ZkDataContext db, int? clanID)
		{
			var planets = AccessiblePlanets(db, clanID, null, true);
			var unlocks =
				planets.SelectMany(x => x.PlanetStructures).Where(x => !x.IsDestroyed && x.StructureType.EffectUnlockID != null).Select(
					x => x.StructureType.Unlock).Distinct().ToList();
			return unlocks;
		}



		public static List<Planet> AccessiblePlanets(ZkDataContext db, int? clanID, AllyStatus? allyStatus = null, bool? researchTreaty = null)
		{

			var milAlly = (from treaty in db.TreatyOffers.Where(x => x.OfferingClanID == clanID)
										 where (allyStatus == null || treaty.AllyStatus == allyStatus) && (researchTreaty == null || treaty.IsResearchAgreement == researchTreaty)
										 join tr2 in db.TreatyOffers on treaty.TargetClanID equals tr2.OfferingClanID
										 where (allyStatus == null || tr2.AllyStatus == allyStatus) && (researchTreaty == null || tr2.IsResearchAgreement == researchTreaty)
										 select treaty.TargetClanID).ToList();

			var gal = db.Galaxies.Single(x => x.IsDefault);
			var planets = gal.Planets.Where(x => x.Account != null && (x.Account.ClanID == clanID || milAlly.Contains(x.Account.ClanID ?? 0)));
			var accesiblePlanets = new List<Planet>();

			foreach (var thisPlanet in planets) {
				var thisPlanetID = thisPlanet.PlanetID;
				accesiblePlanets.Add(thisPlanet);

				// iterate links to this planet
				foreach (var link in gal.Links.Where(l => (l.PlanetID1 == thisPlanetID || l.PlanetID2 == thisPlanetID) && l.LinktStrength > 0)) {
					var otherPlanet = thisPlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;
					accesiblePlanets.Add(otherPlanet);
				}
			}
			return accesiblePlanets;
		}



		static public void RecalculateShadowInfluence(ZkDataContext db)
		{
			var gal = db.Galaxies.Single(x => x.IsDefault);
			foreach (var thisPlanet in gal.Planets)
			{
				var thisPlanetID = thisPlanet.PlanetID;
				var thisLinkStrenght = thisPlanet.PlanetStructures.Where(s => !s.IsDestroyed).Sum(s => s.StructureType.EffectLinkStrength) ?? 0;

				// clear shadow influence
				foreach (var thisAccountPlanet in thisPlanet.AccountPlanets) thisAccountPlanet.ShadowInfluence = 0;

				// set shadow influence

				// iterate links to this planet
				foreach (var link in gal.Links.Where(l => l.PlanetID1 == thisPlanetID || l.PlanetID2 == thisPlanetID))
				{
					var otherPlanet = thisPlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;
					var otherLinkStrenght = otherPlanet.PlanetStructures.Where(s => !s.IsDestroyed).Sum(s => s.StructureType.EffectLinkStrength) ?? 0;

					// increment shadow influence of player on the other side of the link
					var influenceFactor = (thisLinkStrenght + otherLinkStrenght) / 2.0;
					if (thisLinkStrenght == 0 || otherLinkStrenght == 0) influenceFactor = 0;

					link.LinktStrength = influenceFactor;


					if (otherPlanet.OwnerAccountID == null) continue; // no owner: planet can't project shadow influence
					if (otherPlanet.Account.ClanID == null) continue; // no clan: can't project influence
					

					// iterate accountPlanets on other side of the link
					foreach (var otherAccountPlanet in otherPlanet.AccountPlanets.Where(ap => ap.Account.ClanID == otherPlanet.Account.ClanID && ap.Influence > 0))
					{
						var otherAccountID = otherAccountPlanet.AccountID;
						// get corresponding accountPlanet on this side of the link
						var thisAccountPlanet = thisPlanet.AccountPlanets.SingleOrDefault(ap => ap.AccountID == otherAccountID);
						if (thisAccountPlanet == null)
						{
							thisAccountPlanet = new AccountPlanet { AccountID = otherAccountID, PlanetID = thisPlanetID };
							db.AccountPlanets.InsertOnSubmit(thisAccountPlanet);
						}

						thisAccountPlanet.ShadowInfluence += (int)(otherAccountPlanet.Influence*influenceFactor);
					}

					
				}
			}
			db.SubmitChanges();
		}
	}
}
