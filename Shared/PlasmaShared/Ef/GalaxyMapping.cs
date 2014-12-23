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
    // Galaxy
    internal partial class GalaxyMapping : EntityTypeConfiguration<Galaxy>
    {
        public GalaxyMapping(string schema = "dbo")
        {
            ToTable(schema + ".Galaxy");
            HasKey(x => x.GalaxyID);

            Property(x => x.GalaxyID).HasColumnName("GalaxyID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Started).HasColumnName("Started").IsOptional();
            Property(x => x.Turn).HasColumnName("Turn").IsRequired();
            Property(x => x.TurnStarted).HasColumnName("TurnStarted").IsOptional();
            Property(x => x.ImageName).HasColumnName("ImageName").IsOptional().HasMaxLength(100);
            Property(x => x.IsDirty).HasColumnName("IsDirty").IsRequired();
            Property(x => x.Width).HasColumnName("Width").IsRequired();
            Property(x => x.Height).HasColumnName("Height").IsRequired();
            Property(x => x.IsDefault).HasColumnName("IsDefault").IsRequired();
            Property(x => x.AttackerSideCounter).HasColumnName("AttackerSideCounter").IsRequired();
            Property(x => x.AttackerSideChangeTime).HasColumnName("AttackerSideChangeTime").IsOptional();
            Property(x => x.MatchMakerState).HasColumnName("MatchMakerState").IsOptional().IsUnicode(false).HasMaxLength(2147483647);
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
