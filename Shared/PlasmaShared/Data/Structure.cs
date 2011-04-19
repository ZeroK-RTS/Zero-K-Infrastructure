using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class PlanetStructure
	{
		public string GetImageUrl()
		{
			if (IsDestroyed) return string.Format("/img/structures/{0}", StructureType.DestroyedMapIcon); 
			else return string.Format("/img/structures/{0}", StructureType.MapIcon);
		}

	}
}
