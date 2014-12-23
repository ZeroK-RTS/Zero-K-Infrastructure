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
    // CommanderDecoration
    internal partial class CommanderDecorationMapping : EntityTypeConfiguration<CommanderDecoration>
    {
        public CommanderDecorationMapping(string schema = "dbo")
        {
            ToTable(schema + ".CommanderDecoration");
            HasKey(x => new { x.CommanderID, x.SlotID });

            Property(x => x.CommanderID).HasColumnName("CommanderID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.DecorationUnlockID).HasColumnName("DecorationUnlockID").IsRequired();
            Property(x => x.SlotID).HasColumnName("SlotID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            // Foreign keys
            HasRequired(a => a.Commander).WithMany(b => b.CommanderDecorations).HasForeignKey(c => c.CommanderID); // FK_CommanderDecoration_Commander
            HasRequired(a => a.Unlock).WithMany(b => b.CommanderDecorations).HasForeignKey(c => c.DecorationUnlockID); // FK_CommanderDecoration_Unlock
            HasRequired(a => a.CommanderDecorationSlot).WithMany(b => b.CommanderDecorations).HasForeignKey(c => c.SlotID); // FK_CommanderDecoration_CommanderDecorationSlot
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
