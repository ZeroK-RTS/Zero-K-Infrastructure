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
    // CommanderSlot
    internal partial class CommanderSlotMapping : EntityTypeConfiguration<CommanderSlot>
    {
        public CommanderSlotMapping(string schema = "dbo")
        {
            ToTable(schema + ".CommanderSlot");
            HasKey(x => x.CommanderSlotID);

            Property(x => x.CommanderSlotID).HasColumnName("CommanderSlotID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.MorphLevel).HasColumnName("MorphLevel").IsRequired();
            Property(x => x.UnlockType).HasColumnName("UnlockType").IsRequired();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
