using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ZkData
{
    public class ForumThread
    {
        
        public ForumThread()
        {
            Created = DateTime.UtcNow;
            LastPost = DateTime.UtcNow;

            Clans = new HashSet<Clan>();
            ForumThreadLastReads = new HashSet<ForumThreadLastRead>();
            Missions = new HashSet<Mission>();
            News = new HashSet<News>();
            Planets = new HashSet<Planet>();
            Resources = new HashSet<Resource>();
            SpringBattles = new HashSet<SpringBattle>();
            ForumPosts = new HashSet<ForumPost>();
        }

        public int ForumThreadID { get; set; }
        [Required]
        [StringLength(300)]
        [Index]
        public string Title { get; set; }

        [Index]
        [MaxLength(100)]
        public string WikiKey { get; set; }

        public DateTime Created { get; set; }
        public int? CreatedAccountID { get; set; }
        public DateTime? LastPost { get; set; }
        public int? LastPostAccountID { get; set; }
        public int PostCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsLocked { get; set; }
        public int? ForumCategoryID { get; set; }
        public bool IsPinned { get; set; }
        public int? RestrictedClanID { get; set; }

        public virtual Account AccountByCreatedAccountID { get; set; }
        public virtual Account AccountByLastPostAccountID { get; set; }
        public virtual ICollection<Clan> Clans { get; set; }
        public virtual Clan Clan { get; set; }
        public virtual ForumCategory ForumCategory { get; set; }
        public virtual ICollection<ForumThreadLastRead> ForumThreadLastReads { get; set; }
        public virtual ICollection<Mission> Missions { get; set; }
        public virtual ICollection<News> News { get; set; }
        public virtual ICollection<Planet> Planets { get; set; }
        public virtual ICollection<Resource> Resources { get; set; }
        public virtual ICollection<SpringBattle> SpringBattles { get; set; }
        public virtual ICollection<ForumPost> ForumPosts { get; set; }


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
                else unreadPostID = ForumPosts.OrderByDescending(x => x.ForumPostID).Select(x => x.ForumPostID).FirstOrDefault();

                lastRead.LastRead = time;
                if (isPost) lastRead.LastPosted = time;
            }
            return unreadPostID;
        }
    }
}
