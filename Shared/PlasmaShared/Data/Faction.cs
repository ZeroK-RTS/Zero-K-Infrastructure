using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
    partial class Faction
    {

        public string GetImageUrl() {
            return string.Format("/img/factions/{0}", ImageFile);
        }

        public static string FactionColor(Faction fac, int myFactionID) {
            if (fac == null) return "";
            return fac.Color;
        }
    }
}
