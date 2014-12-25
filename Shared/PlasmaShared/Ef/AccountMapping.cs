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
    // Account
    internal partial class AccountMapping : EntityTypeConfiguration<Account>
    {
        public AccountMapping(string schema = "dbo")
        {
            ToTable(schema + ".Account");
            HasKey(x => x.AccountID);

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name).HasColumnName("Name").IsRequired().IsUnicode(false).HasMaxLength(200);
            Property(x => x.Email).HasColumnName("Email").IsOptional();
            Property(x => x.FirstLogin).HasColumnName("FirstLogin").IsRequired();
            Property(x => x.LastLogin).HasColumnName("LastLogin").IsRequired();
            Property(x => x.Aliases).HasColumnName("Aliases").IsOptional();
            Property(x => x.Elo).HasColumnName("Elo").IsRequired();
            Property(x => x.EloWeight).HasColumnName("EloWeight").IsRequired();
            Property(x => x.Elo1v1).HasColumnName("Elo1v1").IsRequired();
            Property(x => x.Elo1v1Weight).HasColumnName("Elo1v1Weight").IsRequired();
            Property(x => x.EloPw).HasColumnName("EloPw").IsRequired();
            Property(x => x.IsLobbyAdministrator).HasColumnName("IsLobbyAdministrator").IsRequired();
            Property(x => x.IsBot).HasColumnName("IsBot").IsRequired();
            Property(x => x.Password).HasColumnName("Password").IsOptional().IsUnicode(false).HasMaxLength(100);
            Property(x => x.Country).HasColumnName("Country").IsOptional().IsUnicode(false).HasMaxLength(5);
            Property(x => x.LobbyTimeRank).HasColumnName("LobbyTimeRank").IsRequired();
            Property(x => x.MissionRunCount).HasColumnName("MissionRunCount").IsRequired();
            Property(x => x.IsZeroKAdmin).HasColumnName("IsZeroKAdmin").IsRequired();
            Property(x => x.XP).HasColumnName("XP").IsRequired();
            Property(x => x.Level).HasColumnName("Level").IsRequired();
            Property(x => x.ClanID).HasColumnName("ClanID").IsOptional();
            Property(x => x.LastNewsRead).HasColumnName("LastNewsRead").IsOptional();
            Property(x => x.FactionID).HasColumnName("FactionID").IsOptional();
            Property(x => x.LobbyID).HasColumnName("LobbyID").IsOptional();
            Property(x => x.IsDeleted).HasColumnName("IsDeleted").IsRequired();
            Property(x => x.Avatar).HasColumnName("Avatar").IsOptional().HasMaxLength(50);
            Property(x => x.SpringieLevel).HasColumnName("SpringieLevel").IsRequired();
            Property(x => x.LobbyVersion).HasColumnName("LobbyVersion").IsOptional().HasMaxLength(200);
            Property(x => x.PwDropshipsProduced).HasColumnName("PwDropshipsProduced").IsRequired();
            Property(x => x.PwDropshipsUsed).HasColumnName("PwDropshipsUsed").IsRequired();
            Property(x => x.PwBombersProduced).HasColumnName("PwBombersProduced").IsRequired();
            Property(x => x.PwBombersUsed).HasColumnName("PwBombersUsed").IsRequired();
            Property(x => x.PwMetalProduced).HasColumnName("PwMetalProduced").IsRequired();
            Property(x => x.PwMetalUsed).HasColumnName("PwMetalUsed").IsRequired();
            Property(x => x.PwWarpProduced).HasColumnName("PwWarpProduced").IsRequired();
            Property(x => x.PwWarpUsed).HasColumnName("PwWarpUsed").IsRequired();
            Property(x => x.PwAttackPoints).HasColumnName("PwAttackPoints").IsRequired();
            Property(x => x.LastLobbyVersionCheck).HasColumnName("LastLobbyVersionCheck").IsOptional();
            Property(x => x.Language).HasColumnName("Language").IsOptional().HasMaxLength(2);
            Property(x => x.HasVpnException).HasColumnName("HasVpnException").IsRequired();
            Property(x => x.Kudos).HasColumnName("Kudos").IsRequired();
            Property(x => x.ForumTotalUpvotes).HasColumnName("ForumTotalUpvotes").IsRequired();
            Property(x => x.ForumTotalDownvotes).HasColumnName("ForumTotalDownvotes").IsRequired();
            Property(x => x.VotesAvailable).HasColumnName("VotesAvailable").IsOptional();
            Property(x => x.SteamID).HasColumnName("SteamID").IsOptional();
            Property(x => x.SteamName).HasColumnName("SteamName").IsOptional().HasMaxLength(200);

            // Foreign keys
            HasOptional(a => a.Faction).WithMany(b => b.Accounts).HasForeignKey(c => c.FactionID); // FK_Account_Faction
            HasMany(t => t.Events).WithMany(t => t.Accounts).Map(m => 
            {
                m.ToTable("EventAccount", schema);
                m.MapLeftKey("AccountID");
                m.MapRightKey("EventID");
            });
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
