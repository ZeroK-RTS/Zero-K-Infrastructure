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
    // ResourceDependency
    internal partial class ResourceDependencyMapping : EntityTypeConfiguration<ResourceDependency>
    {
        public ResourceDependencyMapping(string schema = "dbo")
        {
            ToTable(schema + ".ResourceDependency");
            HasKey(x => new { x.ResourceID, x.NeedsInternalName });

            Property(x => x.ResourceID).HasColumnName("ResourceID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.NeedsInternalName).HasColumnName("NeedsInternalName").IsRequired().IsUnicode(false).HasMaxLength(250).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            // Foreign keys
            HasRequired(a => a.Resource).WithMany(b => b.ResourceDependencies).HasForeignKey(c => c.ResourceID); // FK_ResourceDependency_Resource
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
