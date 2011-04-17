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
