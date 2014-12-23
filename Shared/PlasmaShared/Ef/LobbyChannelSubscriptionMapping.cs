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
    // LobbyChannelSubscription
    internal partial class LobbyChannelSubscriptionMapping : EntityTypeConfiguration<LobbyChannelSubscription>
    {
        public LobbyChannelSubscriptionMapping(string schema = "dbo")
        {
            ToTable(schema + ".LobbyChannelSubscription");
            HasKey(x => new { x.AccountID, x.Channel });

            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Channel).HasColumnName("Channel").IsRequired().HasMaxLength(100).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.LobbyChannelSubscriptions).HasForeignKey(c => c.AccountID); // FK_LobbyChannelSubscription_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
