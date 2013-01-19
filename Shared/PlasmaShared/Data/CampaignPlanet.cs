using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ZkData
{
	partial class CampaignPlanet
	{
		public const double OverlayRatio = 2.25;

	    public override string ToString() {
	        return Name;
	    }

        //fixme
	    public string GetColor(Account viewer)
		{
            var db = new ZkDataContext();
            var progress = db.AccountCampaignProgress.First(x => x.AccountID == viewer.AccountID && x.PlanetID == PlanetID);
            if (progress == null) return "#808080";
            bool isUnlocked = progress.IsUnlocked || StartsUnlocked;
            bool isCompleted = progress.IsCompleted;

            if (isCompleted) return "#00FF00";
            else if (isUnlocked) return "#FFFF00";
            else return "#808080";
		}

		public Rectangle PlanetOverlayRectangle(Campaign camp)
		{
			var w = Mission.Resources.PlanetWarsIconSize*OverlayRatio;
			var xp = (int)(X*camp.MapWidth);
			var yp = (int)(Y*camp.MapHeight);
			return new Rectangle((int)(xp - w/2), (int)(yp - w/2), (int)w, (int)w);
		}

        public Rectangle PlanetRectangle(Campaign camp)
		{
			var w = Mission.Resources.PlanetWarsIconSize;
            var xp = (int)(X * camp.MapWidth);
            var yp = (int)(Y * camp.MapHeight);
			return new Rectangle((int)(xp - w/2), (int)(yp - w/2), (int)w, (int)w);
		}
	}
}