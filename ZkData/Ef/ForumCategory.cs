using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public enum ForumMode
    {
        General = 0,
        News = 1,
        Maps = 2,
        Missions = 3,
        SpringBattles = 4,
        Clans = 5,
        Planets = 6,
        Wiki = 7,
        Archive = 8,
    }
    public class ForumCategory
    {
       
        public int ForumCategoryID { get; set; }
        [Required]
        [StringLength(500)]
        public string Title { get; set; }
        public int? ParentForumCategoryID { get; set; }
        public int SortOrder { get; set; }
        public bool IsLocked { get; set; }
        public ForumMode  ForumMode { get; set; }

        public virtual ICollection<ForumCategory> ChildForumCategories { get; set; } = new HashSet<ForumCategory>();
        public virtual ForumCategory ParentForumCategory { get; set; }
        public virtual ICollection<ForumLastRead> ForumLastReads { get; set; } = new HashSet<ForumLastRead>();
        public virtual ICollection<ForumThread> ForumThreads { get; set; } = new HashSet<ForumThread>();


        public List<ForumCategory> GetPath() {
            var ret = new List<ForumCategory>();
            ret.Add(this);
            var p = this.ParentForumCategory;
            while (p != null)
            {
                ret.Add(p);
                p = p.ParentForumCategory;
            }
            ret.Reverse();
            return ret;
        }
    }
}
