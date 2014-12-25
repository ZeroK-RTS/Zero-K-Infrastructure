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
    internal partial class ForumPostMapping : EntityTypeConfiguration<ForumPost>
    {
        public ForumPostMapping(string schema = "dbo")
        {
            ToTable(schema + ".ForumPost");
            HasKey(x => x.ForumPostID);

            Property(x => x.ForumPostID).HasColumnName("ForumPostID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.AuthorAccountID).HasColumnName("AuthorAccountID").IsRequired();
            Property(x => x.Created).HasColumnName("Created").IsRequired();
            Property(x => x.Text).HasColumnName("Text").IsRequired();
            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsRequired();
            Property(x => x.Upvotes).HasColumnName("Upvotes").IsRequired();
            Property(x => x.Downvotes).HasColumnName("Downvotes").IsRequired();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
