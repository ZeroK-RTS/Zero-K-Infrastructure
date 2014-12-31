namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ForumCategory")]
    public partial class ForumCategory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ForumCategory()
        {
            ForumCategory1 = new HashSet<ForumCategory>();
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ForumCategory> ForumCategory1 { get; set; }

        public virtual ForumCategory ForumCategory2 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ForumLastRead> ForumLastReads { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ForumThread> ForumThreads { get; set; }
    }
}
