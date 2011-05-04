using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ZkData
{
	

	partial class Planet
	{
		public const double OverlayRatio = 2.25;

		public int GetIPToCapture()
		{
			var ownerIP = 0;
			if (Account != null && Account.ClanID != null)
			{
				ownerIP = AccountPlanets.Where(x =>  x.Account.ClanID == Account.ClanID).Sum(x => (int?)(x.Influence + x.ShadowInfluence)) ?? 0;
			}
			ownerIP += PlanetStructures.Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectInfluenceDefense) ?? 0;

			return ownerIP;
		}

		public Rectangle PlanetRectangle(Galaxy gal)
		{
			var w = Resource.PlanetWarsIconSize;
			var xp = (int)(X * gal.Width);
			var yp = (int)(Y * gal.Height);
			return new Rectangle((int)(xp - w / 2), (int)(yp - w / 2), (int)w, (int)w);
		}


		public string GetColor(Account viewer)
		{
            if (Account == null || Account.Clan == null)
            {
				return "gray";
            }
            else if (viewer != null && viewer.Clan != null)
            {
                return Clan.TreatyColor(viewer.Clan, Account.Clan);
            }
            else
            { 
                return "blue";
            }
		}



		public Rectangle PlanetOverlayRectangle(Galaxy gal)
		{
			var w = Resource.PlanetWarsIconSize *OverlayRatio;
			var xp = (int)(X * gal.Width);
			var yp = (int)(Y * gal.Height);
			return new Rectangle((int)(xp - w/2), (int)(yp - w/2), (int)w, (int)w);
		}
	}
}
