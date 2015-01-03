using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class ForumCategory
    {
        
        public ForumCategory()
        {
            ChildForumCategories = new HashSet<ForumCategory>();
            ForumLastReads = new HashSet<ForumLastRead>();
            ForumThreads = new HashSet<ForumThread>();
        }

        public int ForumCategoryID { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        public int? ParentForumCategoryID { get; set; }

        public bool IsLocked { get; set; }

        public bool IsMissions { get; set; }

        public bool IsMaps { get; set; }

        public int SortOrder { get; set; }

        public bool IsSpringBattles { get; set; }

        public bool IsClans { get; set; }

        public bool IsPlanets { get; set; }

        public bool IsNews { get; set; }

        
        public virtual ICollection<ForumCategory> ChildForumCategories { get; set; }

        public virtual ForumCategory ParentForumCategory { get; set; }

        
        public virtual ICollection<ForumLastRead> ForumLastReads { get; set; }

        
        public virtual ICollection<ForumThread> ForumThreads { get; set; }
    }
}
