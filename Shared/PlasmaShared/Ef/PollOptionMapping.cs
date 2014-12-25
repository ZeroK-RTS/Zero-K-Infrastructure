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
    // PollOption
    internal partial class PollOptionMapping : EntityTypeConfiguration<PollOption>
    {
        public PollOptionMapping(string schema = "dbo")
        {
            ToTable(schema + ".PollOption");
            HasKey(x => x.OptionID);

            Property(x => x.OptionID).HasColumnName("OptionID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.PollID).HasColumnName("PollID").IsRequired();
            Property(x => x.OptionText).HasColumnName("OptionText").IsRequired().HasMaxLength(200);
            Property(x => x.Votes).HasColumnName("Votes").IsRequired();

            // Foreign keys
            HasRequired(a => a.Poll).WithMany(b => b.PollOptions).HasForeignKey(c => c.PollID); // FK_PollOption_Poll
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
