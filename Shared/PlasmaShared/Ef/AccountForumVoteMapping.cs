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
    // AccountForumVote
    internal partial class AccountForumVoteMapping : EntityTypeConfiguration<AccountForumVote>
    {
        public AccountForumVoteMapping(string schema = "dbo")
        {
            ToTable(schema + ".AccountForumVote");
            HasKey(x => new { x.AccountID, x.ForumPostID });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.ForumPostID).HasColumnName("ForumPostID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Vote).HasColumnName("Vote").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.AccountForumVotes).HasForeignKey(c => c.AccountID); // FK_AccountForumVote_Account
            HasRequired(a => a.ForumPost).WithMany(b => b.AccountForumVotes).HasForeignKey(c => c.ForumPostID); // FK_AccountForumVote_ForumPost
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
