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
    // RoleTypeHierarchy
    internal partial class RoleTypeHierarchyMapping : EntityTypeConfiguration<RoleTypeHierarchy>
    {
        public RoleTypeHierarchyMapping(string schema = "dbo")
        {
            ToTable(schema + ".RoleTypeHierarchy");
            HasKey(x => new { x.MasterRoleTypeID, x.SlaveRoleTypeID });

            Property(x => x.MasterRoleTypeID).HasColumnName("MasterRoleTypeID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.SlaveRoleTypeID).HasColumnName("SlaveRoleTypeID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.CanAppoint).HasColumnName("CanAppoint").IsRequired();
            Property(x => x.CanRecall).HasColumnName("CanRecall").IsRequired();

            // Foreign keys
            HasRequired(a => a.RoleType_MasterRoleTypeID).WithMany(b => b.RoleTypeHierarchiesByMasterRoleTypeID).HasForeignKey(c => c.MasterRoleTypeID); // FK_RoleTypeHierarchy_RoleType
            HasRequired(a => a.RoleType_SlaveRoleTypeID).WithMany(b => b.RoleTypeHierarchies_SlaveRoleTypeID).HasForeignKey(c => c.SlaveRoleTypeID); // FK_RoleTypeHierarchy_RoleType1
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
