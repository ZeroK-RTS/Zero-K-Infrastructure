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
    // ResourceSpringHash
    internal partial class ResourceSpringHashMapping : EntityTypeConfiguration<ResourceSpringHash>
    {
        public ResourceSpringHashMapping(string schema = "dbo")
        {
            ToTable(schema + ".ResourceSpringHash");
            HasKey(x => new { x.ResourceID, x.SpringVersion });

            Property(x => x.ResourceID).HasColumnName("ResourceID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.SpringVersion).HasColumnName("SpringVersion").IsRequired().HasMaxLength(50).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.SpringHash).HasColumnName("SpringHash").IsRequired();

            // Foreign keys
            HasRequired(a => a.Resource).WithMany(b => b.ResourceSpringHashes).HasForeignKey(c => c.ResourceID); // FK_ResourceSpringHash_Resource
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
