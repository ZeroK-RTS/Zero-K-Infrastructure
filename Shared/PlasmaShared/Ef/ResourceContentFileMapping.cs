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
    // ResourceContentFile
    internal partial class ResourceContentFileMapping : EntityTypeConfiguration<ResourceContentFile>
    {
        public ResourceContentFileMapping(string schema = "dbo")
        {
            ToTable(schema + ".ResourceContentFile");
            HasKey(x => new { x.ResourceID, x.Md5 });

            Property(x => x.ResourceID).HasColumnName("ResourceID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Md5).HasColumnName("Md5").IsRequired().IsFixedLength().IsUnicode(false).HasMaxLength(32).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Length).HasColumnName("Length").IsRequired();
            Property(x => x.FileName).HasColumnName("FileName").IsRequired().HasMaxLength(255);
            Property(x => x.Links).HasColumnName("Links").IsOptional().IsUnicode(false).HasMaxLength(2147483647);
            Property(x => x.LinkCount).HasColumnName("LinkCount").IsRequired();

            // Foreign keys
            HasRequired(a => a.Resource).WithMany(b => b.ResourceContentFiles).HasForeignKey(c => c.ResourceID); // FK_ResourceContentFile_Resource
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
