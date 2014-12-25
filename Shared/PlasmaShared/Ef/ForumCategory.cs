// ReSharper disable RedundantUsingDirective
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable RedundantNameQualifier

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
//using DatabaseGeneratedOption = System.ComponentModel.DataAnnotations.DatabaseGeneratedOption;

namespace ZkData
{
    // ForumCategory
    public partial class ForumCategory
    {
        public int ForumCategoryID { get; set; } // ForumCategoryID (Primary key)
        public string Title { get; set; } // Title
        public int? ParentForumCategoryID { get; set; } // ParentForumCategoryID
        public bool IsLocked { get; set; } // IsLocked
        public bool IsMissions { get; set; } // IsMissions
        public bool IsMaps { get; set; } // IsMaps
        public int SortOrder { get; set; } // SortOrder
        public bool IsSpringBattles { get; set; } // IsSpringBattles
        public bool IsClans { get; set; } // IsClans
        public bool IsPlanets { get; set; } // IsPlanets
        public bool IsNews { get; set; } // IsNews

        // Reverse navigation
        public virtual ICollection<ForumCategory> ForumCategories { get; set; } // ForumCategory.FK_ForumCategory_ForumCategory
        public virtual ICollection<ForumLastRead> ForumLastReads { get; set; } // Many to many mapping
        public virtual ICollection<ForumThread> ForumThreads { get; set; } // ForumThread.FK_ForumThread_ForumCategory

        // Foreign keys
        public virtual ForumCategory ForumCategory_ParentForumCategoryID { get; set; } // FK_ForumCategory_ForumCategory

        public ForumCategory()
        {
            IsLocked = false;
            IsMissions = false;
            IsMaps = false;
            SortOrder = 0;
            IsSpringBattles = false;
            IsClans = false;
            IsPlanets = false;
            IsNews = false;
            ForumCategories = new List<ForumCategory>();
            ForumLastReads = new List<ForumLastRead>();
            ForumThreads = new List<ForumThread>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
