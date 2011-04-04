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

    public int UpdateLastRead(int accountID, bool isPost, DateTime? time = null)
    {
    	int unreadPostID = 0;
      ViewCount++;
      if (accountID > 0)
      {
        if (time == null) time = DateTime.UtcNow;
        var lastRead = ForumThreadLastReads.SingleOrDefault(x => x.AccountID == accountID);
        if (lastRead == null)
        {
          lastRead = new ForumThreadLastRead() { AccountID = accountID };
          ForumThreadLastReads.Add(lastRead);
        }

      	var firstUnreadPost = ForumPosts.FirstOrDefault(x => x.Created > (lastRead.LastRead ?? DateTime.MinValue));
				if (firstUnreadPost != null) unreadPostID = firstUnreadPost.ForumPostID; 

        lastRead.LastRead = time;
        if (isPost) lastRead.LastPosted = time;
      }
    	return unreadPostID;
    }
	}
}
