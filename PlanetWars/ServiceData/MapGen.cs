using System;
using System.Linq;
using ServiceData;

namespace MapGenerator
{
	public class MapGen
	{
		private readonly Random random = new Random();

		public double GaussianRandom(double min, double max) {
			double cnt = 0;
			for (var i = 0; i < 5; i++) {
				cnt += random.NextDouble();
			}
			return min + (max - min)*cnt/5.0;
		}

		private string ToRoman(int i) {
			switch (i) {
				case 1:
					return "I";
				case 2:
					return "II";
				case 3:
					return "III";
				case 4:
					return "IV";
				case 5:
					return "V";
				case 6:
					return "VI";
				case 7:
					return "VII";
				case 8:
					return "VIII";
				case 9:
					return "IX";
				case 10:
					return "X";
			}
			return "";
		}

		public void NewMap() {
			var db = new DbDataContext();

			db.CelestialObjects.DeleteAllOnSubmit(db.CelestialObjects.Where(x => x.CelestialObjectType != CelestialObjectType.Star && (x.ParentCelestialObject.StarSystems == null)));

			db.SubmitChanges();

			var neutralStars = db.CelestialObjects.Where(x => x.CelestialObjectType == CelestialObjectType.Star && x.StarSystems  == null);
			

			foreach (var star in neutralStars) {
				var planetCount = random.Next(3, 10);

				for (int i =0 ; i< planetCount; i++) {
          
					int distance = 0;
					var ok = false;
					while (!ok) {
						distance = random.Next(5, 100);
						ok = true;
						foreach (var p in star.ChildCelestialObjects) {
							if (Math.Abs(p.OrbitDistance - distance) < 5) {
								ok = false;
								break;
							}
						}
					}
					// todo set proper naming (in order)!

					CelestialObject o = GetRandomObject(CelestialObjectType.Star);
          star.ChildCelestialObjects.Add(o); // orbits star				
          o.Name = string.Format("{0} {1}", star.Name, ToRoman(i+1));

					o.OrbitDistance = distance;
					o.OrbitSpeed = 1 / o.OrbitDistance / 100;
					o.OrbitSpeed = o.OrbitSpeed*(1+GaussianRandom(-30, 30)/100);

					if (o.CelestialObjectType !=  CelestialObjectType.Asteroid) {
						var moonCount = random.Next(3);
						double moonDistance = 0;

						for (int j = 0 ; j < moonCount;j++) {
							var moonStep = GaussianRandom(0.5, 2.5);
							CelestialObject m = GetRandomObject(o.CelestialObjectType);
							o.ChildCelestialObjects.Add(m);
							moonDistance += moonStep;
							m.OrbitDistance = moonDistance;
							m.OrbitSpeed = 1/m.OrbitDistance/300;
							m.OrbitSpeed = m.OrbitSpeed * (1 + GaussianRandom(-30, 30) / 100);

							m.Name = o.Name + (char)('A' + j);

						}
					}
				}

				db.SubmitChanges();
			}
		}

		public CelestialObject CreateHomeworld(CelestialObject star) {
			CelestialObject o = new CelestialObject();
			var cnt = o.ChildCelestialObjects.Count;
			o.CelestialObjectType = CelestialObjectType.Planet;
			o.MetalDensity = 1;
			o.FoodDensity = 1;
			o.Size = 5;
			o.Name = string.Format("{0} {1}", star.Name, ToRoman(cnt + 1));

			o.OrbitInitialAngle = random.NextDouble() * 2 * Math.PI;

			o.OrbitDistance = o.ChildCelestialObjects.Max(x => (double?)x.OrbitDistance)??0 +GaussianRandom(5, 15);
			o.OrbitSpeed = 1/o.OrbitDistance/100;
			o.OrbitSpeed = o.OrbitSpeed * (1 + GaussianRandom(-30, 30) / 100);
			star.ChildCelestialObjects.Add(o);
			return o;
		}

		private CelestialObject GetRandomObject(CelestialObjectType fromType) {
			var o = new CelestialObject();


			o.OrbitInitialAngle = random.NextDouble()*2*Math.PI;

			o.CelestialObjectType = random.Next(3 - (int)fromType) +  fromType+1;

			switch (o.CelestialObjectType) {
				case CelestialObjectType.Planet:
					o.MetalDensity = random.Next(2,16)/10;
					o.FoodDensity = random.Next(0,21)/10;
					o.Size = random.Next(3,7);
					break;

				case CelestialObjectType.Moon:
					o.MetalDensity = random.Next(3,22) / 10;
					o.FoodDensity = random.Next(0,10) / 10;
					o.Size = random.Next(2,5);
					break;

				case CelestialObjectType.Asteroid:
					o.MetalDensity = random.Next(5,35) / 10;
					o.FoodDensity = random.Next(0,5) / 10;
					o.Size = random.Next(1, 4);
					break;
			}
			return o;
		}
	}
}