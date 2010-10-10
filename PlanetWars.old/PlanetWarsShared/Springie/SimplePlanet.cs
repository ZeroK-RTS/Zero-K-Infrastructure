using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace PlanetWarsShared.Springie
{
    [Serializable]
    public class SimplePlanet
    {
        public SimpleFaction Faction { get; set; }
        public string MapName { get; set; }
        public string Name { get; set; }
        public SimplePlayer Owner { get; set; }
    }
}