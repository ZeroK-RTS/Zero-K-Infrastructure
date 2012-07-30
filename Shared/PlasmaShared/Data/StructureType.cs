using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class StructureType
	{

        public string GetImageUrl() {
            return string.Format("/img/structures/{0}", MapIcon);
        }

	    public override string ToString() {
	        return Name;
	    }
	}
}
