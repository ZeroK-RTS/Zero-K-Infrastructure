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
    // PollVote
    internal partial class PollVoteMapping : EntityTypeConfiguration<PollVote>
    {
        public PollVoteMapping(string schema = "dbo")
        {
            ToTable(schema + ".PollVote");
            HasKey(x => new { x.AccountID, x.PollID });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.PollID).HasColumnName("PollID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.OptionID).HasColumnName("OptionID").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.PollVotes).HasForeignKey(c => c.AccountID); // FK_PollVote_Account
            HasRequired(a => a.Poll).WithMany(b => b.PollVotes).HasForeignKey(c => c.PollID); // FK_PollVote_Poll
            HasRequired(a => a.PollOption).WithMany(b => b.PollVotes).HasForeignKey(c => c.OptionID); // FK_PollVote_PollOption
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
