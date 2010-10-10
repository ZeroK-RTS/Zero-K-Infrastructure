using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetWarsShared
{
    [Serializable]
    public class UnitDef
    {
        public string Name { get; set; }
        public string FullName { get; set; }

        public UnitDef() {}

        public UnitDef(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
        }
    }
}
