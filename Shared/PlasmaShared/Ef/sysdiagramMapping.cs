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
    // sysdiagrams
    internal partial class sysdiagramMapping : EntityTypeConfiguration<sysdiagram>
    {
        public sysdiagramMapping(string schema = "dbo")
        {
            ToTable(schema + ".sysdiagrams");
            HasKey(x => x.diagram_id);

            Property(x => x.name).HasColumnName("name").IsRequired().HasMaxLength(128);
            Property(x => x.principal_id).HasColumnName("principal_id").IsRequired();
            Property(x => x.diagram_id).HasColumnName("diagram_id").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.version).HasColumnName("version").IsOptional();
            Property(x => x.definition).HasColumnName("definition").IsOptional();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
