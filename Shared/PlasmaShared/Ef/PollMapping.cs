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
    // Poll
    internal partial class PollMapping : EntityTypeConfiguration<Poll>
    {
        public PollMapping(string schema = "dbo")
        {
            ToTable(schema + ".Poll");
            HasKey(x => x.PollID);

            Property(x => x.PollID).HasColumnName("PollID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.QuestionText).HasColumnName("QuestionText").IsRequired().HasMaxLength(500);
            Property(x => x.IsAnonymous).HasColumnName("IsAnonymous").IsRequired();
            Property(x => x.RoleTypeID).HasColumnName("RoleTypeID").IsOptional();
            Property(x => x.RoleTargetAccountID).HasColumnName("RoleTargetAccountID").IsOptional();
            Property(x => x.RoleIsRemoval).HasColumnName("RoleIsRemoval").IsRequired();
            Property(x => x.RestrictFactionID).HasColumnName("RestrictFactionID").IsOptional();
            Property(x => x.RestrictClanID).HasColumnName("RestrictClanID").IsOptional();
            Property(x => x.CreatedAccountID).HasColumnName("CreatedAccountID").IsOptional();
            Property(x => x.ExpireBy).HasColumnName("ExpireBy").IsOptional();
            Property(x => x.IsHeadline).HasColumnName("IsHeadline").IsRequired();

            // Foreign keys
            HasOptional(a => a.RoleType).WithMany(b => b.Polls).HasForeignKey(c => c.RoleTypeID); // FK_Poll_RoleType
            HasOptional(a => a.Account_RoleTargetAccountID).WithMany(b => b.Polls_RoleTargetAccountID).HasForeignKey(c => c.RoleTargetAccountID); // FK_Poll_Account
            HasOptional(a => a.Faction).WithMany(b => b.Polls).HasForeignKey(c => c.RestrictFactionID); // FK_Poll_Faction
            HasOptional(a => a.Account_CreatedAccountID).WithMany(b => b.Polls_CreatedAccountID).HasForeignKey(c => c.CreatedAccountID); // FK_Poll_Account1
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
