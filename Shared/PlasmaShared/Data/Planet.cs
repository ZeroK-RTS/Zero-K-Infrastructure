using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ZkData
{
	

	partial class Planet
	{


		public Rectangle PlanetRectangle(Galaxy gal)
		{
			var w = Resource.PlanetWarsIconSize;
			var xp = (int)(X * gal.Width);
			var yp = (int)(Y * gal.Height);
			return new Rectangle(xp - w/2, yp - w/2, w, w);
		}
	}
}
