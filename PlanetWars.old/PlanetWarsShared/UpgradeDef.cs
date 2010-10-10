#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

#endregion

namespace PlanetWarsShared
{
    [Serializable]
    public class UpgradeDef
    {
        const int StructurePlacementTolerance = 10;
        public static int GlobalID;
        public List<DeployLocation> DeployLocations = new List<DeployLocation>();
        public int Died;
        public int Purchased;
        public int QuantityMobiles;
        public UpgradeDef() {}

        public UpgradeDef(string division,
                          string branch,
                          int level,
                          string factionName,
                          string description,
                          List<UnitDef> unitDefs)
        {
            Division = division;
            Branch = branch;
            Level = level;
            Description = description;
            FactionName = factionName;
            UnitDefs = unitDefs;
            ID = GlobalID++;
        }

        public int ID { get; set; }
        public string Division { get; set; }
        public string Branch { get; set; }
        public int Level { get; set; }
        public string Description { get; set; }
        public string FactionName { get; set; }

        public bool IsSpaceShip
        {
            get { return Division == "Spacefleets"; }
        }

        public List<UnitDef> UnitDefs { get; set; }

        public string UnitChoiceHumanReadable
        {
            get
            {
                var p = UnitDefs.Where(x => x.Name == UnitChoice).SingleOrDefault();
                if (p != null) {
                    return string.Format("{0}", p.FullName);
                }
                return null;
            }
        }

        public int Cost
        {
            get { return (1 << (Level - 1))*100; }
        }

        public bool IsBuilding
        {
            get { return Division == "Buildings"; }
        }

        [XmlIgnore]
        public int QuantityDeployed
        {
            get { return DeployLocations.Count; }
        }

        public string UnitChoice { get; set; }

        public UpgradeDef BuyCopy()
        {
            var clone = (UpgradeDef)MemberwiseClone();
            clone.DeployLocations = new List<DeployLocation>();
            return clone;
        }

        public void KillStructure(int x, int z, int planetID)
        {
            var deploy = FindStructure(planetID, x, z);
            if (deploy == null) {
                throw new Exception(string.Format("Cannot find selected deploy {0} {1} on planet {2}", x, z, planetID));
            }
            DeployLocations.Remove(deploy);
        }

        public DeployLocation FindStructure(int planetID, int x, int z)
        {
            return
                DeployLocations.Where(
                    d =>
                    d.PlanetID == planetID && Math.Abs(d.X - x) < StructurePlacementTolerance &&
                    Math.Abs(d.Z - z) < StructurePlacementTolerance).SingleOrDefault();
        }

        public void AddStructure(int x, int z, int planetID, string orientation)
        {
            var deploy = FindStructure(planetID, x, z);
            if (deploy != null) {
                throw new Exception(string.Format("Unit on {0},{1} is already deployed!", x, z));
            }
            DeployLocations.Add(new DeployLocation(orientation, x, z, planetID));
        }

        public void DeleteAllFromPlanet(int planetID)
        {
            DeployLocations.RemoveAll(x => x.PlanetID == planetID);
        }

        [Serializable]
        public class DeployLocation
        {
            public string Orientation;
            public int PlanetID;
            public int X;
            public int Z;
            public DeployLocation() {}

            public DeployLocation(string orientation, int x, int z, int planetID)
            {
                Orientation = orientation;
                X = x;
                Z = z;
                PlanetID = planetID;
            }
        }
    }
}