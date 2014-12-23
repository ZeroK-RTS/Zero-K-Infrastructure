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
    // ForumCategory
    internal partial class ForumCategoryMapping : EntityTypeConfiguration<ForumCategory>
    {
        public ForumCategoryMapping(string schema = "dbo")
        {
            ToTable(schema + ".ForumCategory");
            HasKey(x => x.ForumCategoryID);

            Property(x => x.ForumCategoryID).HasColumnName("ForumCategoryID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Title).HasColumnName("Title").IsRequired().HasMaxLength(500);
            Property(x => x.ParentForumCategoryID).HasColumnName("ParentForumCategoryID").IsOptional();
            Property(x => x.IsLocked).HasColumnName("IsLocked").IsRequired();
            Property(x => x.IsMissions).HasColumnName("IsMissions").IsRequired();
            Property(x => x.IsMaps).HasColumnName("IsMaps").IsRequired();
            Property(x => x.SortOrder).HasColumnName("SortOrder").IsRequired();
            Property(x => x.IsSpringBattles).HasColumnName("IsSpringBattles").IsRequired();
            Property(x => x.IsClans).HasColumnName("IsClans").IsRequired();
            Property(x => x.IsPlanets).HasColumnName("IsPlanets").IsRequired();
            Property(x => x.IsNews).HasColumnName("IsNews").IsRequired();

            // Foreign keys
            HasOptional(a => a.ForumCategory_ParentForumCategoryID).WithMany(b => b.ForumCategories).HasForeignKey(c => c.ParentForumCategoryID); // FK_ForumCategory_ForumCategory
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
