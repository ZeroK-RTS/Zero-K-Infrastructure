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
    // News
    public partial class News
    {
        public int NewsID { get; set; } // NewsID (Primary key)
        public DateTime Created { get; set; } // Created
        public string Title { get; set; } // Title
        public string Text { get; set; } // Text
        public int AuthorAccountID { get; set; } // AuthorAccountID
        public DateTime HeadlineUntil { get; set; } // HeadlineUntil
        public int ForumThreadID { get; set; } // ForumThreadID
        public int? SpringForumPostID { get; set; } // SpringForumPostID
        public string ImageExtension { get; set; } // ImageExtension
        public string ImageContentType { get; set; } // ImageContentType
        public int? ImageLength { get; set; } // ImageLength

        // Foreign keys
        public virtual Account Account { get; set; } // FK_News_Account
        public virtual ForumThread ForumThread { get; set; } // FK_News_ForumThread
    }

}
