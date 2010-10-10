using System;
using System.Linq;

namespace ServiceData
{
	partial class CelestialObject
	{
		public int GetShipCount(int shipTypeID)
		{
			var entry = CelestialObjectShips.SingleOrDefault(x => x.ShipTypeID == shipTypeID);
			if (entry != null) return entry.Count;
			else return 0;
		}

		public int GetStructureCount(int structureTypeID)
		{
			var entry = CelestialObjectStructures.SingleOrDefault(x => x.StructureTypeID == structureTypeID);
			if (entry != null) return entry.Count;
			else return 0;
		}

		public string GetNameWithOwner()
		{
			if (Player != null) return string.Format("{0}'s {1}", Player.Name, Name);
			else return Name;
		}

		public string GetName()
		{
			return Name;
		}

    public Position GetPosition(int gameSecond, int secondPerTurn)
		{
			if (OrbitDistance <=0 ) return new Position(X, Y);
			else {
				var parent =ParentCelestialObject.GetPosition(gameSecond, secondPerTurn);
				var turns = gameSecond / secondPerTurn;
				var theta = OrbitInitialAngle + OrbitSpeed*turns*2*Math.PI;
        X = parent.X +OrbitDistance*Math.Cos(theta);
        Y = parent.Y + OrbitDistance*Math.Sin(theta);
				return new Position(X,Y);
			}
		}

		public void SetShipCount(int shipTypeID, int count)
		{
			var entry = CelestialObjectShips.SingleOrDefault(x => x.ShipTypeID == shipTypeID);
			if (entry == null) {
				entry = new CelestialObjectShip();
				entry.ShipTypeID = shipTypeID;
				CelestialObjectShips.Add(entry);
			}
			entry.Count = count;
			if (count == 0) CelestialObjectShips.Remove(entry);
		}

		public void SetStructureCount(int structureTypeID, int count)
		{
			var entry = CelestialObjectStructures.SingleOrDefault(x => x.StructureTypeID == structureTypeID);
			if (entry == null) {
				entry = new CelestialObjectStructure();
				entry.StructureTypeID = structureTypeID;
				CelestialObjectStructures.Add(entry);
			}
			entry.Count = count;
			if (count == 0) CelestialObjectStructures.Remove(entry);
		}

		public void UpdateIncomeInfo()
		{
			if (OwnerID == null) return; //Dont update neutral planet data

			MaxPopulation = 0;
			FoodIncome = 0;
			MetalIncome = 0;
			QuantiumIncome = 0;
			DarkMatterIncome = 0;
			ResearchIncome = 0;
			Buildpower = 0;

			var needsPeople = 0;

			foreach (var entry in CelestialObjectStructures) {
				var s = entry.StructureType;
				var cnt = entry.Count;
				MetalIncome += s.MakesMetal*MetalDensity*cnt;
				FoodIncome += s.MakesFood*FoodDensity*cnt;
				QuantiumIncome += s.MakesMetal*QuantiumDensity*cnt;
				DarkMatterIncome += s.MakesMetal*DarkMatterDensity*cnt;
				ResearchIncome += s.MakesResearch*cnt;
				MaxPopulation += s.StoresPeople*cnt;
				MaxPopulation += s.NeedsPeople*cnt;
				needsPeople += s.NeedsPeople*cnt;
				Buildpower += s.BuildsMetal*cnt;
			}

			var mothership = Players.SingleOrDefault(x => x.HomeworldID == CelestialObjectID && (x.Transit == null || x.Transit.OrbitsObjectID == CelestialObjectID)); // mothership orbits this world

			if (mothership != null) {
				foreach (var entry in mothership.MothershipStructures) {
					var s = entry.StructureType;
					var cnt = entry.Count;

					MetalIncome += s.MakesMetal*MetalDensity*cnt;
					FoodIncome += s.MakesFood*FoodDensity*cnt;
					QuantiumIncome += s.MakesMetal*QuantiumDensity*cnt;
					DarkMatterIncome += s.MakesMetal*DarkMatterDensity*cnt;
					ResearchIncome += s.MakesResearch*cnt;
					MaxPopulation += s.StoresPeople*cnt;
					MaxPopulation += s.NeedsPeople*cnt;
					needsPeople += s.NeedsPeople*cnt;
					Buildpower += s.BuildsMetal*cnt;
				}
			}

			if (needsPeople > 0) Efficiency = 0.2 + 0.8 * ((double)Population / needsPeople);
			else Efficiency = 1;
		}
	}
}