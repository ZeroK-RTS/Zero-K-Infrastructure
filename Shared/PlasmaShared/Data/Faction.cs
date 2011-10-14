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

        public string GetShipImageUrl() {
            return string.Format("/img/factions/{0}_ship.png", Shortcut);
        }

        public static string FactionColor(Faction fac, int myFactionID) {
            if (fac == null) return "";
            return fac.Color;
        }
    }
}
