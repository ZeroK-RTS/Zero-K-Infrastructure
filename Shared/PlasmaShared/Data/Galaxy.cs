using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace ZkData
{
	partial class Galaxy
	{
		static public void RecalculateShadowInfluence()
		{
			using (var scope = new TransactionScope())
			using (var db = new ZkDataContext())
			{
				foreach (var thisPlanet in db.Planets)
				{
					var thisPlanetID = thisPlanet.PlanetID;
					var thisLinkStrenght = thisPlanet.PlanetStructures.Max(s => s.StructureType.EffectLinkStrength) ?? 0;

					// clear shadow influence
					foreach (var thisAccountPlanet in thisPlanet.AccountPlanets) thisAccountPlanet.ShadowInfluence = 0;

					// set shadow influence

					// iterate links to this planet
					foreach (var link in db.Links.Where(l => l.PlanetID1 == thisPlanetID || l.PlanetID2 == thisPlanetID))
					{
						var otherPlanet = thisPlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;
						var otherLinkStrenght = otherPlanet.PlanetStructures.Max(s => s.StructureType.EffectLinkStrength) ?? 0;

						// iterate accountPlanets on other side of the link
						foreach (var otherAccountPlanet in otherPlanet.AccountPlanets)
						{
							if (otherAccountPlanet.Influence > 0)
							{
		
								var otherAccountID = otherAccountPlanet.AccountID;
								
								// get corresponding accountPlanet on this side of the link
								var thisAccountPlanet = thisPlanet.AccountPlanets.SingleOrDefault(ap => ap.AccountID == otherAccountID);
								if (thisAccountPlanet == null)
								{
									thisAccountPlanet = new AccountPlanet { AccountID = otherAccountID, PlanetID = thisPlanetID };
									db.AccountPlanets.InsertOnSubmit(thisAccountPlanet);
								}

								// increment shadow influence of player on the other side of the link
								var influenceFactor = (thisLinkStrenght + otherLinkStrenght) / 2.0;
								if (thisLinkStrenght == 0 || otherLinkStrenght == 0) influenceFactor = 0;
								thisAccountPlanet.ShadowInfluence += (int)(otherAccountPlanet.Influence * influenceFactor);
							}
						}
					}
				}
				// TODO: make planets flip side?
				db.SubmitChanges();
				scope.Complete();
			}
		}
	}
}
