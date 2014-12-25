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
    // RoleType
    internal partial class RoleTypeMapping : EntityTypeConfiguration<RoleType>
    {
        public RoleTypeMapping(string schema = "dbo")
        {
            ToTable(schema + ".RoleType");
            HasKey(x => x.RoleTypeID);

            Property(x => x.RoleTypeID).HasColumnName("RoleTypeID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name).HasColumnName("Name").IsRequired().HasMaxLength(50);
            Property(x => x.Description).HasColumnName("Description").IsRequired().HasMaxLength(500);
            Property(x => x.IsClanOnly).HasColumnName("IsClanOnly").IsRequired();
            Property(x => x.IsOnePersonOnly).HasColumnName("IsOnePersonOnly").IsRequired();
            Property(x => x.RestrictFactionID).HasColumnName("RestrictFactionID").IsOptional();
            Property(x => x.IsVoteable).HasColumnName("IsVoteable").IsRequired();
            Property(x => x.PollDurationDays).HasColumnName("PollDurationDays").IsRequired();
            Property(x => x.RightDropshipQuota).HasColumnName("RightDropshipQuota").IsRequired();
            Property(x => x.RightBomberQuota).HasColumnName("RightBomberQuota").IsRequired();
            Property(x => x.RightMetalQuota).HasColumnName("RightMetalQuota").IsRequired();
            Property(x => x.RightWarpQuota).HasColumnName("RightWarpQuota").IsRequired();
            Property(x => x.RightDiplomacy).HasColumnName("RightDiplomacy").IsRequired();
            Property(x => x.RightEditTexts).HasColumnName("RightEditTexts").IsRequired();
            Property(x => x.RightSetEnergyPriority).HasColumnName("RightSetEnergyPriority").IsRequired();
            Property(x => x.RightKickPeople).HasColumnName("RightKickPeople").IsRequired();
            Property(x => x.DisplayOrder).HasColumnName("DisplayOrder").IsRequired();

            // Foreign keys
            HasOptional(a => a.Faction).WithMany(b => b.RoleTypes).HasForeignKey(c => c.RestrictFactionID); // FK_RoleType_Faction
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
