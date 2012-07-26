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

		




	}

}
