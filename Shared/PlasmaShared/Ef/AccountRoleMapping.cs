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
    // AccountRole
    internal partial class AccountRoleMapping : EntityTypeConfiguration<AccountRole>
    {
        public AccountRoleMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountRole");
            HasKey(x => new { x.AccountID, x.RoleTypeID });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.RoleTypeID).HasColumnName("RoleTypeID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Inauguration).HasColumnName("Inauguration").IsRequired();
            Property(x => x.FactionID).HasColumnName("FactionID").IsOptional();
            Property(x => x.ClanID).HasColumnName("ClanID").IsOptional();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.AccountRolesByAccountID).HasForeignKey(c => c.AccountID); // FK_AccountRole_Account
            HasRequired(a => a.RoleType).WithMany(b => b.AccountRoles).HasForeignKey(c => c.RoleTypeID); // FK_AccountRole_RoleType
            HasOptional(a => a.Faction).WithMany(b => b.AccountRoles).HasForeignKey(c => c.FactionID); // FK_AccountRole_Faction
            HasOptional(a => a.Clan).WithMany(b => b.AccountRoles).HasForeignKey(c => c.ClanID); // FK_AccountRole_Clan
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
