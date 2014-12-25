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
    // ForumPostEdit
    internal partial class ForumPostEditMapping : EntityTypeConfiguration<ForumPostEdit>
    {
        public ForumPostEditMapping(string schema = "dbo")
        {
            ToTable(schema + ".ForumPostEdit");
            HasKey(x => x.ForumPostEditID);

            Property(x => x.ForumPostEditID).HasColumnName("ForumPostEditID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.ForumPostID).HasColumnName("ForumPostID").IsRequired();
            Property(x => x.EditorAccountID).HasColumnName("EditorAccountID").IsRequired();
            Property(x => x.OriginalText).HasColumnName("OriginalText").IsOptional();
            Property(x => x.NewText).HasColumnName("NewText").IsOptional();
            Property(x => x.EditTime).HasColumnName("EditTime").IsRequired();

            // Foreign keys
            HasRequired(a => a.ForumPost).WithMany(b => b.ForumPostEdits).HasForeignKey(c => c.ForumPostID); // FK_ForumPostEdit_ForumPost
            HasRequired(a => a.Account).WithMany(b => b.ForumPostEdits).HasForeignKey(c => c.EditorAccountID); // FK_ForumPostEdit_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
