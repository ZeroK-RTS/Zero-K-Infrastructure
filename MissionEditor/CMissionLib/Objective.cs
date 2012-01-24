using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib
{
	[DataContract]
	public class Objective : PropertyChanged
	{
        [DataMember]
        public string ID { get; set; }
	}
}
