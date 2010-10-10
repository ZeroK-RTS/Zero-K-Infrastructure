using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace PlanetWars
{
	public class CelestialObject
	{
		public double X;
		public double Y;
		public IEnumerable<CelestialObject> Children;
		public double OrbitDistance;
		public int OrbitNestingLevel;
		public string Name;
		public int Size;
		public CelestialObject ParentObject { get;set; }

		public override string ToString()
		{
			return Name;
		}

		public Brush Brush
		{
			get
			{
				switch (CelestialObjectType)
				{
					case CelestialObjectType.Planet:
						return new SolidColorBrush(Colors.Cyan);
					case CelestialObjectType.Moon:
						return new SolidColorBrush(Colors.White);
					case CelestialObjectType.Asteroid:
						return new SolidColorBrush(Colors.Red);
					case CelestialObjectType.Star:
						return new SolidColorBrush(Colors.Yellow);
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
		public CelestialObjectType CelestialObjectType;
		public int CelestialObjectID;
		public double OrbitInitialAngle;
		public double OrbitSpeed;
		public int OwnerID;

		public Color Color
		{
			get
			{
				switch (CelestialObjectType)
				{
					case CelestialObjectType.Planet:
						return Colors.Cyan;
					case CelestialObjectType.Moon:
						return Colors.White;
					case CelestialObjectType.Asteroid:
						return Colors.Red;
					case CelestialObjectType.Star:
						return Colors.Yellow;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

/*		public Player Owner
		{
			get { return StarMap.GetOwner(this); }
		}
		public string OwnerName
		{
			get { return Owner == null ? "Unclaimed" : Owner.Name; }
		}
		public CelestialObject Parent { get; set; }
		public CelestialObject ParentStar { get; set; }
		public StarMap StarMap { get; set; }*/

	}

	public enum CelestialObjectType
	{
		Star = 0,
		Planet = 1,
		Moon = 2,
		Asteroid = 3
	}

}
