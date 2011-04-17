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
		public string TreatyColor(int? otherClanID)
		{
			var tr = this.TreatyOffersByOfferingClanID.FirstOrDefault(x => x.TargetClanID == otherClanID);
			Clan clan2 = null;
			if (tr != null) clan2 = tr.ClanByTargetClanID;
			return TreatyColor(this, clan2);
		}


		public bool CanJoin(Account account)
		{
			if (account == null) return true;
			if (account.LobbyTimeRank > 0 && Accounts.Count >= GlobalConst.MaxClanSkilledSize) return false;
			else return true;
		}


		public string GetImageUrl()
		{
			return string.Format("/img/clans/{0}.png", ClanID);
		}


		public static string TreatyColor(Clan clan1, Clan clan2)
		{
			if (clan1 == null || clan2 == null) return "#FFFFFF";
			if (clan1 == clan2) return "#00FFFF";
			var t = clan1.GetEffectiveTreaty(clan2);
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

		public EffectiveTreaty GetEffectiveTreaty(Clan secondClan)
		{
			var t1 = this.TreatyOffersByOfferingClanID.FirstOrDefault(x => x.TargetClanID == secondClan.ClanID);
			var t2 = secondClan.TreatyOffersByOfferingClanID.FirstOrDefault(x => x.TargetClanID == this.ClanID);

			var ret = new EffectiveTreaty();
			if (t1 != null && t2 != null)
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
