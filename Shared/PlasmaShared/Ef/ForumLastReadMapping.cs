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
    // ForumLastRead
    internal partial class ForumLastReadMapping : EntityTypeConfiguration<ForumLastRead>
    {
        public ForumLastReadMapping(string schema = "dbo")
        {
            ToTable(schema + ".ForumLastRead");
            HasKey(x => new { x.AccountID, x.ForumCategoryID });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.ForumCategoryID).HasColumnName("ForumCategoryID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.LastRead).HasColumnName("LastRead").IsOptional();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.ForumLastReads).HasForeignKey(c => c.AccountID); // FK_ForumLastRead_Account
            HasRequired(a => a.ForumCategory).WithMany(b => b.ForumLastReads).HasForeignKey(c => c.ForumCategoryID); // FK_ForumLastRead_ForumCategory
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
