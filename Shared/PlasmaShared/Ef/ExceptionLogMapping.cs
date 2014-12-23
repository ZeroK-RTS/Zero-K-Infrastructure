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
    // ExceptionLog
    internal partial class ExceptionLogMapping : EntityTypeConfiguration<ExceptionLog>
    {
        public ExceptionLogMapping(string schema = "dbo")
        {
            ToTable(schema + ".ExceptionLog");
            HasKey(x => x.ExceptionLogID);

            Property(x => x.ExceptionLogID).HasColumnName("ExceptionLogID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.ProgramID).HasColumnName("ProgramID").IsRequired();
            Property(x => x.Exception).HasColumnName("Exception").IsRequired();
            Property(x => x.ExtraData).HasColumnName("ExtraData").IsOptional();
            Property(x => x.RemoteIP).HasColumnName("RemoteIP").IsOptional().HasMaxLength(50);
            Property(x => x.PlayerName).HasColumnName("PlayerName").IsOptional().HasMaxLength(200);
            Property(x => x.Time).HasColumnName("Time").IsRequired();
            Property(x => x.ProgramVersion).HasColumnName("ProgramVersion").IsOptional().HasMaxLength(100);
            Property(x => x.ExceptionHash).HasColumnName("ExceptionHash").IsRequired().IsFixedLength().IsUnicode(false).HasMaxLength(32);
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
