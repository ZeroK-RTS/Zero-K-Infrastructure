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
    // CampaignVar
    internal partial class CampaignVarMapping : EntityTypeConfiguration<CampaignVar>
    {
        public CampaignVarMapping(string schema = "dbo")
        {
            ToTable(schema + ".CampaignVar");
            HasKey(x => new { x.CampaignID, x.VarID });

            Property(x => x.CampaignID).HasColumnName("CampaignID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.VarID).HasColumnName("VarID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.KeyString).HasColumnName("KeyString").IsRequired().HasMaxLength(50);
            Property(x => x.Description).HasColumnName("Description").IsOptional();

            // Foreign keys
            HasRequired(a => a.Campaign).WithMany(b => b.CampaignVars).HasForeignKey(c => c.CampaignID); // FK_CampaignVar_Campaign
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
