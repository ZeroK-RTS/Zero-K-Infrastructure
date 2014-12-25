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
    // Punishment
    internal partial class PunishmentMapping : EntityTypeConfiguration<Punishment>
    {
        public PunishmentMapping(string schema = "dbo")
        {
            ToTable(schema + ".Punishment");
            HasKey(x => x.PunishmentID);

            Property(x => x.PunishmentID).HasColumnName("PunishmentID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired();
            Property(x => x.Reason).HasColumnName("Reason").IsRequired().HasMaxLength(1000);
            Property(x => x.Time).HasColumnName("Time").IsRequired();
            Property(x => x.BanExpires).HasColumnName("BanExpires").IsOptional();
            Property(x => x.BanMute).HasColumnName("BanMute").IsRequired();
            Property(x => x.BanCommanders).HasColumnName("BanCommanders").IsRequired();
            Property(x => x.BanUnlocks).HasColumnName("BanUnlocks").IsRequired();
            Property(x => x.BanSite).HasColumnName("BanSite").IsRequired();
            Property(x => x.BanLobby).HasColumnName("BanLobby").IsRequired();
            Property(x => x.BanIP).HasColumnName("BanIP").IsOptional().HasMaxLength(1000);
            Property(x => x.BanForum).HasColumnName("BanForum").IsRequired();
            Property(x => x.UserID).HasColumnName("UserID").IsOptional();
            Property(x => x.CreatedAccountID).HasColumnName("CreatedAccountID").IsOptional();
            Property(x => x.DeleteInfluence).HasColumnName("DeleteInfluence").IsRequired();
            Property(x => x.DeleteXP).HasColumnName("DeleteXP").IsRequired();
            Property(x => x.SegregateHost).HasColumnName("SegregateHost").IsRequired();
            Property(x => x.SetRightsToZero).HasColumnName("SetRightsToZero").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account_AccountID).WithMany(b => b.Punishments_AccountID).HasForeignKey(c => c.AccountID); // FK_Punishment_Account
            HasOptional(a => a.Account_CreatedAccountID).WithMany(b => b.Punishments_CreatedAccountID).HasForeignKey(c => c.CreatedAccountID); // FK_Punishment_Account1
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
