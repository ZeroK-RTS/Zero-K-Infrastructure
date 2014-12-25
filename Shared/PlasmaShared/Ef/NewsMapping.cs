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
    internal partial class NewsMapping : EntityTypeConfiguration<News>
    {
        public NewsMapping(string schema = "dbo")
        {
            ToTable(schema + ".News");
            HasKey(x => x.NewsID);

            Property(x => x.NewsID).HasColumnName("NewsID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Created).HasColumnName("Created").IsRequired();
            Property(x => x.Title).HasColumnName("Title").IsRequired().HasMaxLength(200);
            Property(x => x.Text).HasColumnName("Text").IsRequired().IsUnicode(false).HasMaxLength(2147483647);
            Property(x => x.AuthorAccountID).HasColumnName("AuthorAccountID").IsRequired();
            Property(x => x.HeadlineUntil).HasColumnName("HeadlineUntil").IsRequired();
            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsRequired();
            Property(x => x.SpringForumPostID).HasColumnName("SpringForumPostID").IsOptional();
            Property(x => x.ImageExtension).HasColumnName("ImageExtension").IsOptional().HasMaxLength(50);
            Property(x => x.ImageContentType).HasColumnName("ImageContentType").IsOptional().HasMaxLength(50);
            Property(x => x.ImageLength).HasColumnName("ImageLength").IsOptional();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.News).HasForeignKey(c => c.AuthorAccountID); // FK_News_Account
            HasRequired(a => a.ForumThread).WithMany(b => b.News).HasForeignKey(c => c.ForumThreadID); // FK_News_ForumThread
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
