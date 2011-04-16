using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class Planet
	{
		public void Detach()
		{
			PropertyChanged = null;
			PropertyChanging = null;
		}
	}
}
