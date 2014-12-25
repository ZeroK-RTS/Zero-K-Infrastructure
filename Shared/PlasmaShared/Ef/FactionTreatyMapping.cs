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
    // FactionTreaty
    internal partial class FactionTreatyMapping : EntityTypeConfiguration<FactionTreaty>
    {
        public FactionTreatyMapping(string schema = "dbo")
        {
            ToTable(schema + ".FactionTreaty");
            HasKey(x => x.FactionTreatyID);

            Property(x => x.FactionTreatyID).HasColumnName("FactionTreatyID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.ProposingFactionID).HasColumnName("ProposingFactionID").IsRequired();
            Property(x => x.ProposingAccountID).HasColumnName("ProposingAccountID").IsRequired();
            Property(x => x.AcceptingFactionID).HasColumnName("AcceptingFactionID").IsRequired();
            Property(x => x.AcceptedAccountID).HasColumnName("AcceptedAccountID").IsOptional();
            Property(x => x.TurnsRemaining).HasColumnName("TurnsRemaining").IsOptional();
            Property(x => x.TreatyState).HasColumnName("TreatyState").IsRequired();
            Property(x => x.TurnsTotal).HasColumnName("TurnsTotal").IsOptional();
            Property(x => x.TreatyNote).HasColumnName("TreatyNote").IsOptional();

            // Foreign keys
            HasRequired(a => a.FactionByProposingFactionID).WithMany(b => b.FactionTreaties_ProposingFactionID).HasForeignKey(c => c.ProposingFactionID); // FK_FactionTreaty_Faction
            HasRequired(a => a.AccountByProposingAccountID).WithMany(b => b.FactionTreaties_ProposingAccountID).HasForeignKey(c => c.ProposingAccountID); // FK_FactionTreaty_Account
            HasRequired(a => a.FactionByAcceptingFactionID).WithMany(b => b.FactionTreaties_AcceptingFactionID).HasForeignKey(c => c.AcceptingFactionID); // FK_FactionTreaty_Faction1
            HasOptional(a => a.Account_AcceptedAccountID).WithMany(b => b.FactionTreaties_AcceptedAccountID).HasForeignKey(c => c.AcceptedAccountID); // FK_FactionTreaty_Account1
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
