using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ZkData
{
	

	partial class Planet
	{
		// TODO: update for shadow influence
		public int GetIPToCapture()
		{
			var ownerIP = 0;
			if (Account != null)
			{
				ownerIP = Account.Clan.Accounts.SelectMany(x => x.AccountPlanets.Where(y => y.PlanetID == PlanetID)).Sum(x => (int?)x.Influence) ??0;
			}
			ownerIP += PlanetStructures.Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectInfluenceDefense) ?? 0;

			return ownerIP;
		}

		public Rectangle PlanetRectangle(Galaxy gal)
		{
			var w = Resource.PlanetWarsIconSize;
			var xp = (int)(X * gal.Width);
			var yp = (int)(Y * gal.Height);
			return new Rectangle(xp - w/2, yp - w/2, w, w);
		}
	}
}
