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
    // Clan
    internal partial class ClanMapping : EntityTypeConfiguration<Clan>
    {
        public ClanMapping(string schema = "dbo")
        {
            ToTable(schema + ".Clan");
            HasKey(x => x.ClanID);

            Property(x => x.ClanID).HasColumnName("ClanID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.ClanName).HasColumnName("ClanName").IsRequired().IsUnicode(false).HasMaxLength(50);
            Property(x => x.Description).HasColumnName("Description").IsOptional().IsUnicode(false).HasMaxLength(500);
            Property(x => x.Password).HasColumnName("Password").IsOptional().IsUnicode(false).HasMaxLength(20);
            Property(x => x.SecretTopic).HasColumnName("SecretTopic").IsOptional().HasMaxLength(500);
            Property(x => x.Shortcut).HasColumnName("Shortcut").IsRequired().IsUnicode(false).HasMaxLength(6);
            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsOptional();
            Property(x => x.IsDeleted).HasColumnName("IsDeleted").IsRequired();
            Property(x => x.FactionID).HasColumnName("FactionID").IsOptional();

            // Foreign keys
            HasOptional(a => a.ForumThread).WithMany(b => b.Clans).HasForeignKey(c => c.ForumThreadID); // FK_Clan_ForumThread
            HasOptional(a => a.Faction).WithMany(b => b.Clans).HasForeignKey(c => c.FactionID); // FK_Clan_Faction
            HasMany(t => t.Events).WithMany(t => t.Clans).Map(m => 
            {
                m.ToTable("EventClan", schema);
                m.MapLeftKey("ClanID");
                m.MapRightKey("EventID");
            });
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
