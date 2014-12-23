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

namespace PlasmaShared.Ef
{
    // ForumThreadLastRead
    internal partial class ForumThreadLastReadMapping : EntityTypeConfiguration<ForumThreadLastRead>
    {
        public ForumThreadLastReadMapping(string schema = "dbo")
        {
            ToTable(schema + ".ForumThreadLastRead");
            HasKey(x => new { x.ForumThreadID, x.AccountID });

            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.LastRead).HasColumnName("LastRead").IsOptional();
            Property(x => x.LastPosted).HasColumnName("LastPosted").IsOptional();

            // Foreign keys
            HasRequired(a => a.ForumThread).WithMany(b => b.ForumThreadLastReads).HasForeignKey(c => c.ForumThreadID); // FK_ForumThreadLastRead_ForumThread
            HasRequired(a => a.Account).WithMany(b => b.ForumThreadLastReads).HasForeignKey(c => c.AccountID); // FK_ForumThreadLastRead_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
