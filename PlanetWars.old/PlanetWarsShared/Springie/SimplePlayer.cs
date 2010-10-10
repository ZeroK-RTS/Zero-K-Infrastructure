using System;
using System.Collections.Generic;
using System.Text;

namespace PlanetWarsShared.Springie
{
    [Serializable]
    public class SimplePlayer
    {
        public string Name { get; set; }
        public Faction Faction { get; set; }
    }
}