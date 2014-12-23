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
    // CommanderDecorationSlot
    internal partial class CommanderDecorationSlotMapping : EntityTypeConfiguration<CommanderDecorationSlot>
    {
        public CommanderDecorationSlotMapping(string schema = "dbo")
        {
            ToTable(schema + ".CommanderDecorationSlot");
            HasKey(x => x.CommanderDecorationSlotID);

            Property(x => x.CommanderDecorationSlotID).HasColumnName("CommanderDecorationSlotID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
