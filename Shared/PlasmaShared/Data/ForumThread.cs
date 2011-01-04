using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class ForumThread
	{
		partial void OnCreated()
		{
			Created = DateTime.UtcNow;
		  LastPost = DateTime.UtcNow;
		}
	}
}
