using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class ForumPost
	{
		partial void OnCreated()
		{
			Created = DateTime.UtcNow;
		}


	}
}
