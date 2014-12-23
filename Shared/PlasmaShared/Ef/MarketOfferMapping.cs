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
    // MarketOffer
    internal partial class MarketOfferMapping : EntityTypeConfiguration<MarketOffer>
    {
        public MarketOfferMapping(string schema = "dbo")
        {
            ToTable(schema + ".MarketOffer");
            HasKey(x => x.OfferID);

            Property(x => x.OfferID).HasColumnName("OfferID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired();
            Property(x => x.PlanetID).HasColumnName("PlanetID").IsRequired();
            Property(x => x.Quantity).HasColumnName("Quantity").IsRequired();
            Property(x => x.Price).HasColumnName("Price").IsRequired();
            Property(x => x.IsSell).HasColumnName("IsSell").IsRequired();
            Property(x => x.DatePlaced).HasColumnName("DatePlaced").IsOptional();
            Property(x => x.DateAccepted).HasColumnName("DateAccepted").IsOptional();
            Property(x => x.AcceptedAccountID).HasColumnName("AcceptedAccountID").IsOptional();

            // Foreign keys
            HasRequired(a => a.Account_AccountID).WithMany(b => b.MarketOffers_AccountID).HasForeignKey(c => c.AccountID); // FK_MarketOffer_Player
            HasRequired(a => a.Planet).WithMany(b => b.MarketOffers).HasForeignKey(c => c.PlanetID); // FK_MarketOffer_Planet
            HasOptional(a => a.Account_AcceptedAccountID).WithMany(b => b.MarketOffers_AcceptedAccountID).HasForeignKey(c => c.AcceptedAccountID); // FK_MarketOffer_Player1
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
