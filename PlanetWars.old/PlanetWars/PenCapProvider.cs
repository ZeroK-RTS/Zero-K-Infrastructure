using System.Drawing;
using System.Drawing.Drawing2D;

/*

Design Pattern Automation Toolkit.
Application to create applications with emphasis on Design patterns.
And support for round trip engineering.
Copyright (C) 2004 Vineeth Neelakant. nvineeth@gmail.com

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

*/

namespace PlanetWarsShared
{
	public class PenCapProvider
	{

		static CustomLineCap multiCap, aggrCap, inhCap, singleCap;
		static AdjustableArrowCap arwCap = new AdjustableArrowCap(6.5f, 5, true),
			inhArwCap = new AdjustableArrowCap(7, 7);
		static PenCapProvider()
		{
			GraphicsPath path;//let they be garbage collected :)

			#region Multiple representation code
			path = new GraphicsPath();
			path.StartFigure();

			path.AddPolygon(new PointF[] { new PointF(0, 0), new PointF(2.0f, 6), new PointF(4, 0) });

			path.AddEllipse(new Rectangle(1, 6, 2, 2));
			Matrix translateMatrix = new Matrix();
			translateMatrix.Translate(-2, -8);
			path.Transform(translateMatrix);
			multiCap = new CustomLineCap(null, path);
			multiCap.BaseInset = 8;
			#endregion

			#region Diamond Cap code
			path = new GraphicsPath();
			path.StartFigure();
			path.AddPolygon(new PointF[] { new PointF(0, 2.5f), new PointF(2.5f, 5), new PointF(5, 2.5f), new PointF(2.5f, 0) });
			path.CloseFigure();
			translateMatrix = new Matrix();
			translateMatrix.Translate(-2.5f, -5.0f);
			path.Transform(translateMatrix);

			aggrCap = new CustomLineCap(null, path);
			aggrCap.BaseInset = 5;
			#endregion


			#region single representation code
			path = new GraphicsPath();
			path.StartFigure();

			path.AddPolygon(new PointF[] { new PointF(0, 0), new PointF(2.0f, 6), new PointF(4, 0) });

			translateMatrix = new Matrix();
			translateMatrix.Translate(-2, -6);
			path.Transform(translateMatrix);

			singleCap = new CustomLineCap(null, path);
			singleCap.BaseInset = 6;
			#endregion

			#region inheritance Cap
			path = new GraphicsPath();
			path.StartFigure();

			path.AddPolygon(new Point[] { new Point(0, 6), new Point(3, 0), new Point(6, 6) });
			path.AddLine(3, 6, 3, 25);

			translateMatrix = new Matrix();
			translateMatrix.Translate(-3.0f, -20);
			path.Transform(translateMatrix);

			inhCap = new CustomLineCap(null, path);
			inhCap.BaseInset = 20;

			#endregion



		}
		public static CustomLineCap GetMultipleRepresentation()//returns a arrow + a circle
		{
			return multiCap;

		}
		public static CustomLineCap GetSingleRepresentation()//returns a arrow cap
		{
			//return arwCap;
			return singleCap;
		}

		public static CustomLineCap GetDiamondRepresentation()
		{
			return aggrCap;

		}

		public static CustomLineCap GetInheritanceRepresentation()
		{
			return inhCap;
			//return inhArwCap;

		}

		public static CustomLineCap GetInheritanceArrowRepresentation()
		{
			return inhArwCap;

		}

	}
}