using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	public enum AllyStatus
	{
		War = -1,
		Neutral = 0,
		Ceasefire = 1,
		Alliance = 2
	}
	partial class Clan
	{


		public static List<Planet> AccessiblePlanets(ZkDataContext db, int? clanID)
		{
			var milAlly = (from treaty in db.TreatyOffers.Where(x=>x.OfferingClanID == clanID)
										 where treaty.AllyStatus == AllyStatus.Alliance
										 join tr2 in db.TreatyOffers on treaty.TargetClanID equals tr2.OfferingClanID
										 where tr2.AllyStatus == AllyStatus.Alliance
										 select treaty.TargetClanID).ToList();

			var planets = db.Planets.Where(x => x.Account.ClanID == clanID || milAlly.Contains(x.Account.ClanID ?? 0));
			var accesiblePlanets = new List<Planet>();

			foreach (var thisPlanet in planets) {
				var thisPlanetID = thisPlanet.PlanetID;
				var thisLinkStrenght = thisPlanet.PlanetStructures.Max(s => s.StructureType.EffectLinkStrength) ?? 0;
				accesiblePlanets.Add(thisPlanet);


				// iterate links to this planet
				foreach (var link in db.Links.Where(l => l.PlanetID1 == thisPlanetID || l.PlanetID2 == thisPlanetID)) {
					var otherPlanet = thisPlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;
					var otherLinkStrenght = otherPlanet.PlanetStructures.Max(s => s.StructureType.EffectLinkStrength) ?? 0;
					if (otherLinkStrenght > 0 && thisLinkStrenght > 0) accesiblePlanets.Add(otherPlanet);
				}
			}
			return accesiblePlanets;
		}


		public bool CanJoin(Account account)
		{
			if (account == null) return true;
			if (account.ClanID != null) return false;
			if (account.LobbyTimeRank > 0 && Accounts.Where(x=>x.LobbyTimeRank > 0).Count() >= GlobalConst.MaxClanSkilledSize) return false;
			else return true;
		}


		public string GetImageUrl()
		{
			return string.Format("/img/clans/{0}.png", ClanID);
		}


		public static string TreatyColor(Clan clan1, Clan clan2)
		{
			if (clan1 == null || clan2 == null) return "";
			if (clan1.ClanID  == clan2.ClanID) return "#00FFFF";
			var t = clan1.GetEffectiveTreaty(clan2.ClanID);
			switch (t.AllyStatus) {
				case AllyStatus.Neutral:
					return "#FFFF00";
				case AllyStatus.War:
					return "#FF0000";
				case AllyStatus.Alliance:
					return "#66FF99";
				case AllyStatus.Ceasefire:
					return "#0066FF";
			}
			return "#FFFFFF";
		}

		public EffectiveTreaty GetEffectiveTreaty(int secondClanID)
		{
			var t1 = this.TreatyOffersByOfferingClanID.FirstOrDefault(x => x.TargetClanID == secondClanID);
			var ret = new EffectiveTreaty();
			if (t1 == null) return ret;
			var t2 = t1.ClanByTargetClanID.TreatyOffersByOfferingClanID.FirstOrDefault(x => x.TargetClanID == this.ClanID);
			if (t2 != null)
			{
				ret.AllyStatus = (AllyStatus)Math.Min((int)t1.AllyStatus, (int)t2.AllyStatus);
				ret.IsResearchAgreement = t1.IsResearchAgreement && t2.IsResearchAgreement;
			}
			return ret;
		}

	}

	public class EffectiveTreaty
	{
		public bool IsResearchAgreement;
		public AllyStatus AllyStatus;

	}
}
