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
    // BlockedHost
    internal partial class BlockedHostMapping : EntityTypeConfiguration<BlockedHost>
    {
        public BlockedHostMapping(string schema = "dbo")
        {
            ToTable(schema + ".BlockedHost");
            HasKey(x => x.HostID);

            Property(x => x.HostID).HasColumnName("HostID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.HostName).HasColumnName("HostName").IsRequired();
            Property(x => x.Comment).HasColumnName("Comment").IsOptional();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
