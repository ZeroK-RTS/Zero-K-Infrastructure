using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceData
{
	public enum CelestialObjectType
	{
		[DataMember]
		Star = 0,
		[DataMember]
		Planet = 1,
		[DataMember]
		Moon = 2,
		[DataMember]
		Asteroid = 3
	}
}
