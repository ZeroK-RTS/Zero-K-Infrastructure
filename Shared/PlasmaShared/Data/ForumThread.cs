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

    public void UpdateLastRead(int accountID, bool isPost, DateTime? time = null)
    {
      if (time == null) time = DateTime.UtcNow;
      var lastRead = ForumThreadLastReads.SingleOrDefault(x => x.AccountID == accountID);
      if (lastRead == null) {
        lastRead = new ForumThreadLastRead() { AccountID = accountID };
        ForumThreadLastReads.Add(lastRead);
      }
      lastRead.LastPosted = time;
      lastRead.LastRead = time;
    }
	}
}
