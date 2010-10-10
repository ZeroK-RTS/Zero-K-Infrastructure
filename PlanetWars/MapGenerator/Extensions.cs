using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MapGenerator
{
	public static class Extensions
	{
		public static double Distance(this Point point, Point to)
		{
			int dx = to.X - point.X;
			int dy = to.Y - point.Y;
			return Math.Sqrt(dx*dx + dy*dy);
		}

	}
}
