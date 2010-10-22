using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class Mission
	{

		partial void OnCreated()
		{
			ModifiedTime = DateTime.UtcNow;
			CreatedTime = DateTime.UtcNow;
		}
	}
}
