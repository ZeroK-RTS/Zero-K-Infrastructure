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
    // Planet
    internal partial class PlanetMapping : EntityTypeConfiguration<Planet>
    {
        public PlanetMapping(string schema = "dbo")
        {
            ToTable(schema + ".Planet");
            HasKey(x => x.PlanetID);

            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name).HasColumnName("Name").IsRequired().IsUnicode(false).HasMaxLength(50);
            Property(x => x.X).HasColumnName("X").IsRequired();
            Property(x => x.Y).HasColumnName("Y").IsRequired();
            Property(x => x.MapResourceID).HasColumnName("MapResourceID").IsOptional();
            Property(x => x.OwnerAccountID).HasColumnName("OwnerAccountID").IsOptional();
            Property(x => x.GalaxyID).HasColumnName("GalaxyID").IsRequired();
            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsOptional();
            Property(x => x.OwnerFactionID).HasColumnName("OwnerFactionID").IsOptional();
            Property(x => x.TeamSize).HasColumnName("TeamSize").IsRequired();

            // Foreign keys
            HasOptional(a => a.Resource).WithMany(b => b.Planets).HasForeignKey(c => c.MapResourceID); // FK_Planet_Resource
            HasOptional(a => a.Account).WithMany(b => b.Planets).HasForeignKey(c => c.OwnerAccountID); // FK_Planet_Account
            HasRequired(a => a.Galaxy).WithMany(b => b.Planets).HasForeignKey(c => c.GalaxyID); // FK_Planet_Galaxy
            HasOptional(a => a.ForumThread).WithMany(b => b.Planets).HasForeignKey(c => c.ForumThreadID); // FK_Planet_ForumThread
            HasOptional(a => a.Faction).WithMany(b => b.Planets).HasForeignKey(c => c.OwnerFactionID); // FK_Planet_Faction
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
