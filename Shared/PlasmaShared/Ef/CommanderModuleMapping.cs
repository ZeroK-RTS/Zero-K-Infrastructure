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
    // CommanderModule
    internal partial class CommanderModuleMapping : EntityTypeConfiguration<CommanderModule>
    {
        public CommanderModuleMapping(string schema = "dbo")
        {
            ToTable(schema + ".CommanderModule");
            HasKey(x => new { x.CommanderID, x.SlotID });

            Property(x => x.CommanderID).HasColumnName("CommanderID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.ModuleUnlockID).HasColumnName("ModuleUnlockID").IsRequired();
            Property(x => x.SlotID).HasColumnName("SlotID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            // Foreign keys
            HasRequired(a => a.Commander).WithMany(b => b.CommanderModules).HasForeignKey(c => c.CommanderID); // FK_CommanderModule_Commander
            HasRequired(a => a.Unlock).WithMany(b => b.CommanderModules).HasForeignKey(c => c.ModuleUnlockID); // FK_CommanderModule_Unlock
            HasRequired(a => a.CommanderSlot).WithMany(b => b.CommanderModules).HasForeignKey(c => c.SlotID); // FK_CommanderModule_CommanderSlot
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
