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

	    public string GetColor(Account viewer)
		{
            bool isUnlocked = IsUnlocked(viewer.AccountID);
            bool isCompleted = IsCompleted(viewer.AccountID);

            if (isCompleted)
            {
                return "#00FF88";
            }
            else if (isUnlocked)
            {
                return "#FFFFFF";
            }
            return "#808080";
		}

		public Rectangle PlanetOverlayRectangle(Campaign camp)
		{
            var db = new ZkDataContext();
            Resource map = db.Resources.FirstOrDefault(m => m.InternalName == Mission.Map);  
			var w = map.PlanetWarsIconSize*OverlayRatio;
			var xp = (int)(X*camp.MapWidth);
			var yp = (int)(Y*camp.MapHeight);
			return new Rectangle((int)(xp - w/2), (int)(yp - w/2), (int)w, (int)w);
		}

        public Rectangle PlanetRectangle(Campaign camp)
		{
            var db = new ZkDataContext();
            Resource map = db.Resources.FirstOrDefault(m => m.InternalName == Mission.Map);  
			var w = map.PlanetWarsIconSize;
            var xp = (int)(X * camp.MapWidth);
            var yp = (int)(Y * camp.MapHeight);
			return new Rectangle((int)(xp - w/2), (int)(yp - w/2), (int)w, (int)w);
		}

        public bool IsUnlocked(int accountID)
        {
            if (StartsUnlocked) return true;

            var db = new ZkDataContext();
            AccountCampaignProgress progress = db.AccountCampaignProgress.FirstOrDefault(x => x.AccountID == accountID && x.CampaignID == CampaignID && x.PlanetID == PlanetID);
            return progress.IsUnlocked;
        }

        public bool IsCompleted(int accountID)
        {
            var db = new ZkDataContext();
            AccountCampaignProgress progress = db.AccountCampaignProgress.FirstOrDefault(x => x.AccountID == accountID && x.CampaignID == CampaignID && x.PlanetID == PlanetID);
            return progress.IsCompleted;
        }
	}
}