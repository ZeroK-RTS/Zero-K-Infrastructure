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
			return AllyStatusColor(t.AllyStatus);
		}

		public static string AllyStatusColor(AllyStatus s) {
			switch (s) {
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
				// the above didnt work for -1 war for some reason..
				if (t1.AllyStatus ==AllyStatus.War || t2.AllyStatus == AllyStatus.War)
					ret.AllyStatus =AllyStatus.War;
				
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
