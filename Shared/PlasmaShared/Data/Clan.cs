using System;
using System.Collections.Generic;
using System.Data.Linq;
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
        partial void OnValidate(ChangeAction action) {
            if (action != ChangeAction.Delete) {
                if (!IsShortcutValid(Shortcut)) throw new ApplicationException("Invalid shortcut - can only contain numbers and letters and must be at least one character long");
            
            }
        }

	    public bool CanJoin(Account account)
		{
			if (account == null) return true;
			if (account.ClanID != null) return false;
            if (account.FactionID != null && account.FactionID != FactionID) return false;
			if (Accounts.Count() >= GlobalConst.MaxClanSkilledSize) return false;
			else return true;
		}


	    public static bool IsShortcutValid(string text) {
            return text.All(Char.IsLetterOrDigit) && text.Length > 0 && text.Length <= 8;
        }




	    public string GetImageUrl()
		{
			return string.Format("/img/clans/{0}.png", Shortcut);
		}

        public string GetBGImageUrl()
        {   return string.Format("/img/clans/{0}_bg.png", Shortcut);
        }


        public static string ClanColor(Clan clan, int? myClanID = null)
        {
            if (clan == null) return "";
            //if (clan.ClanID == myClanID) return "#00FFFF";
            return clan.Faction.Color;
        }

		public static string TreatyColor(Clan clan1, Clan clan2)
		{
			if (clan1 == null || clan2 == null) return "";
			if (clan1.ClanID  == clan2.ClanID) return "#00FFFF";
			
            var t = clan1.GetEffectiveTreaty(clan2);
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

		public EffectiveTreaty GetEffectiveTreaty(Clan secondClan)
		{
			if (secondClan == null) return new EffectiveTreaty();
			var t1 = this.TreatyOffersByOfferingClanID.FirstOrDefault(x => x.TargetClanID == secondClan.ClanID);
			var ret = new EffectiveTreaty();
			TreatyOffer t2 = null;
			if (t1 == null) {
				t2 = secondClan.TreatyOffersByOfferingClanID.FirstOrDefault(x => x.TargetClanID == this.ClanID);
				if (t2 != null && t2.AllyStatus == AllyStatus.War) ret.AllyStatus = AllyStatus.War;
			} else {
				t2 = t1.ClanByTargetClanID.TreatyOffersByOfferingClanID.FirstOrDefault(x => x.TargetClanID == this.ClanID);
				if (t1.AllyStatus == AllyStatus.War) ret.AllyStatus = AllyStatus.War;
				if (t2 != null) {
					ret.AllyStatus = (AllyStatus)Math.Min((int)t1.AllyStatus, (int)t2.AllyStatus);
					ret.IsResearchAgreement = t1.IsResearchAgreement && t2.IsResearchAgreement;
				}
			}
            if (ret.AllyStatus== AllyStatus.Neutral)
            {
                if (FactionID == secondClan.FactionID)
                {
                    ret.AllyStatus = AllyStatus.Alliance;
                    ret.IsResearchAgreement = true;
                }
                //else if (FactionID != secondClan.FactionID) ret.AllyStatus = AllyStatus.War;
            }

            ret.InfluenceGivenToSecondClanBalance = (t1 != null ? t1.InfluenceGiven+100.0:100.0) / (t2 != null ? t2.InfluenceGiven +100.0: 100.0);


		    return ret;
		}

		/*
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
		}*/

	}

	public class EffectiveTreaty
	{
		public bool IsResearchAgreement;
		public AllyStatus AllyStatus;
	    public double InfluenceGivenToSecondClanBalance;
	}
}
