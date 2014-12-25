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
    // Unlock
    internal partial class UnlockMapping : EntityTypeConfiguration<Unlock>
    {
        public UnlockMapping(string schema = "dbo")
        {
            ToTable(schema + ".Unlock");
            HasKey(x => x.UnlockID);

            Property(x => x.UnlockID).HasColumnName("UnlockID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Code).HasColumnName("Code").IsRequired().HasMaxLength(100);
            Property(x => x.Name).HasColumnName("Name").IsOptional().HasMaxLength(200);
            Property(x => x.Description).HasColumnName("Description").IsOptional().HasMaxLength(1000);
            Property(x => x.NeededLevel).HasColumnName("NeededLevel").IsRequired();
            Property(x => x.LimitForChassis).HasColumnName("LimitForChassis").IsOptional().HasMaxLength(500);
            Property(x => x.UnlockType).HasColumnName("UnlockType").IsRequired();
            Property(x => x.RequiredUnlockID).HasColumnName("RequiredUnlockID").IsOptional();
            Property(x => x.MorphLevel).HasColumnName("MorphLevel").IsRequired();
            Property(x => x.MaxModuleCount).HasColumnName("MaxModuleCount").IsRequired();
            Property(x => x.MetalCost).HasColumnName("MetalCost").IsOptional();
            Property(x => x.XpCost).HasColumnName("XpCost").IsRequired();
            Property(x => x.MetalCostMorph2).HasColumnName("MetalCostMorph2").IsOptional();
            Property(x => x.MetalCostMorph3).HasColumnName("MetalCostMorph3").IsOptional();
            Property(x => x.MetalCostMorph4).HasColumnName("MetalCostMorph4").IsOptional();
            Property(x => x.MetalCostMorph5).HasColumnName("MetalCostMorph5").IsOptional();
            Property(x => x.KudosCost).HasColumnName("KudosCost").IsOptional();
            Property(x => x.IsKudosOnly).HasColumnName("IsKudosOnly").IsOptional();

            // Foreign keys
            HasOptional(a => a.ParentUnlock).WithMany(b => b.Unlocks).HasForeignKey(c => c.RequiredUnlockID); // FK_Unlock_Unlock
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
