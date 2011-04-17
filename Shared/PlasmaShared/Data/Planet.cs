using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class Planet
	{
		public Point LabelLocation(Galaxy gal)
		{
			var w = Resource.PlanetWarsIconSize;
			return new Point((int)(X * gal.Width -w/2.0) , (int)(Y*gal.Height + w/2.0));
		}
	}
}
