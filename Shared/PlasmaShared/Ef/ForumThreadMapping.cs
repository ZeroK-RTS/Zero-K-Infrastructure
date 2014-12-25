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
    // ForumThread
    internal partial class ForumThreadMapping : EntityTypeConfiguration<ForumThread>
    {
        public ForumThreadMapping(string schema = "dbo")
        {
            ToTable(schema + ".ForumThread");
            HasKey(x => x.ForumThreadID);

            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Title).HasColumnName("Title").IsRequired().HasMaxLength(300);
            Property(x => x.Created).HasColumnName("Created").IsRequired();
            Property(x => x.CreatedAccountID).HasColumnName("CreatedAccountID").IsOptional();
            Property(x => x.LastPost).HasColumnName("LastPost").IsOptional();
            Property(x => x.LastPostAccountID).HasColumnName("LastPostAccountID").IsOptional();
            Property(x => x.PostCount).HasColumnName("PostCount").IsRequired();
            Property(x => x.ViewCount).HasColumnName("ViewCount").IsRequired();
            Property(x => x.IsLocked).HasColumnName("IsLocked").IsRequired();
            Property(x => x.ForumCategoryID).HasColumnName("ForumCategoryID").IsOptional();
            Property(x => x.IsPinned).HasColumnName("IsPinned").IsRequired();
            Property(x => x.RestrictedClanID).HasColumnName("RestrictedClanID").IsOptional();

            // Foreign keys
            HasOptional(a => a.AccountByCreatedAccountID).WithMany(b => b.ForumThreads_CreatedAccountID).HasForeignKey(c => c.CreatedAccountID); // FK_ForumThread_Account
            HasOptional(a => a.Account_LastPostAccountID).WithMany(b => b.ForumThreads_LastPostAccountID).HasForeignKey(c => c.LastPostAccountID); // FK_ForumThread_Account1
            HasOptional(a => a.ForumCategory).WithMany(b => b.ForumThreads).HasForeignKey(c => c.ForumCategoryID); // FK_ForumThread_ForumCategory
            HasOptional(a => a.Clan).WithMany(b => b.ForumThreads).HasForeignKey(c => c.RestrictedClanID); // FK_ForumThread_Clan
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
