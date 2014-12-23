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
    // AccountRatingVote
    internal partial class AccountRatingVoteMapping : EntityTypeConfiguration<AccountRatingVote>
    {
        public AccountRatingVoteMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountRatingVote");
            HasKey(x => new { x.RatingPollID, x.AccountID });

            Property(x => x.RatingPollID).HasColumnName("RatingPollID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Vote).HasColumnName("Vote").IsRequired();

            // Foreign keys
            HasRequired(a => a.RatingPoll).WithMany(b => b.AccountRatingVotes).HasForeignKey(c => c.RatingPollID); // FK_AccountRatingVote_RatingPoll
            HasRequired(a => a.Account).WithMany(b => b.AccountRatingVotes).HasForeignKey(c => c.AccountID); // FK_AccountRatingVote_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
