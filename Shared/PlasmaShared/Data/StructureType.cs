using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class StructureType
	{
		public static bool HasStructureOrUpgrades(ZkDataContext db, Planet planet, StructureType structureType)
		{
			if (planet.PlanetStructures.Any(s => structureType.StructureTypeID == s.StructureTypeID)) return true;
			// has found stucture in tech tree
			if (planet.PlanetStructures.Any(s => structureType.UpgradesToStructureID == s.StructureTypeID)) return true;
			// has reached the end of the tech tree, no structure found
			if (structureType.UpgradesToStructureID == null) return false;
			// search the next step in the tech tree
			return HasStructureOrUpgrades(db, planet, db.StructureTypes.Single(s => s.StructureTypeID == structureType.UpgradesToStructureID));
		}
	}
}
