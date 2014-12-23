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
    // Campaign
    internal partial class CampaignMapping : EntityTypeConfiguration<Campaign>
    {
        public CampaignMapping(string schema = "dbo")
        {
            ToTable(schema + ".Campaign");
            HasKey(x => x.CampaignID);

            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Name).HasColumnName("Name").IsRequired().HasMaxLength(50);
            Property(x => x.Description).HasColumnName("Description").IsOptional();
            Property(x => x.MapWidth).HasColumnName("MapWidth").IsRequired();
            Property(x => x.MapHeight).HasColumnName("MapHeight").IsRequired();
            Property(x => x.IsDirty).HasColumnName("IsDirty").IsRequired();
            Property(x => x.IsHidden).HasColumnName("IsHidden").IsOptional();
            Property(x => x.MapImageName).HasColumnName("MapImageName").IsOptional().HasMaxLength(100);
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
