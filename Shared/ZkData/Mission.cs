using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class Mission
	{
		public string ShortName
		{
			get { return Name.Substring(9); }
		}

	}
}
