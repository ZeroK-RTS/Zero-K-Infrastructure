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
    // CommanderDecorationIcon
    internal partial class CommanderDecorationIconMapping : EntityTypeConfiguration<CommanderDecorationIcon>
    {
        public CommanderDecorationIconMapping(string schema = "dbo")
        {
            ToTable(schema + ".CommanderDecorationIcon");
            HasKey(x => x.DecorationUnlockID);

            Property(x => x.DecorationUnlockID).HasColumnName("DecorationUnlockID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.IconPosition).HasColumnName("IconPosition").IsRequired();
            Property(x => x.IconType).HasColumnName("IconType").IsRequired();

            // Foreign keys
            HasRequired(a => a.Unlock).WithOptional(b => b.CommanderDecorationIcon); // FK_CommanderDecorationIcon_Unlock
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
