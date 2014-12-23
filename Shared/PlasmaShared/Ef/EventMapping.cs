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
    // Event
    internal partial class EventMapping : EntityTypeConfiguration<Event>
    {
        public EventMapping(string schema = "dbo")
        {
            ToTable(schema + ".Event");
            HasKey(x => x.EventID);

            Property(x => x.EventID).HasColumnName("EventID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Text).HasColumnName("Text").IsRequired().HasMaxLength(4000);
            Property(x => x.Time).HasColumnName("Time").IsRequired();
            Property(x => x.Turn).HasColumnName("Turn").IsRequired();
            Property(x => x.PlainText).HasColumnName("PlainText").IsOptional().HasMaxLength(4000);
            HasMany(t => t.Factions).WithMany(t => t.Events).Map(m => 
            {
                m.ToTable("EventFaction", schema);
                m.MapLeftKey("EventID");
                m.MapRightKey("FactionID");
            });
            HasMany(t => t.Planets).WithMany(t => t.Events).Map(m => 
            {
                m.ToTable("EventPlanet", schema);
                m.MapLeftKey("EventID");
                m.MapRightKey("PlanetID");
            });
            HasMany(t => t.SpringBattles).WithMany(t => t.Events).Map(m => 
            {
                m.ToTable("EventSpringBattle", schema);
                m.MapLeftKey("EventID");
                m.MapRightKey("SpringBattleID");
            });
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
