using System.Configuration;
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
    // ForumPost
    public partial class ForumPost
    {
        public int ForumPostID { get; set; } // ForumPostID (Primary key)
        public int AuthorAccountID { get; set; } // AuthorAccountID
        public DateTime Created { get; set; } // Created
        public string Text { get; set; } // Text
        public int ForumThreadID { get; set; } // ForumThreadID
        public int Upvotes { get; set; } // Upvotes
        public int Downvotes { get; set; } // Downvotes

        // Reverse navigation
        public virtual ICollection<AccountForumVote> AccountForumVotes { get; set; } // Many to many mapping
        public virtual ICollection<ForumPostEdit> ForumPostEdits { get; set; } // ForumPostEdit.FK_ForumPostEdit_ForumPost
        public virtual ForumThread ForumThread { get; set; }

        public ForumPost()
        {
            AccountForumVotes = new List<AccountForumVote>();
            ForumPostEdits = new List<ForumPostEdit>();
            Created = DateTime.UtcNow;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
