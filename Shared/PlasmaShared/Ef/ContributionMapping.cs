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
    // Contribution
    internal partial class ContributionMapping : EntityTypeConfiguration<Contribution>
    {
        public ContributionMapping(string schema = "dbo")
        {
            ToTable(schema + ".Contribution");
            HasKey(x => x.ContributionID);

            Property(x => x.ContributionID).HasColumnName("ContributionID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.AccountID).HasColumnName("AccountID").IsOptional();
            Property(x => x.Time).HasColumnName("Time").IsRequired();
            Property(x => x.PayPalTransactionID).HasColumnName("PayPalTransactionID").IsOptional().HasMaxLength(50);
            Property(x => x.Name).HasColumnName("Name").IsOptional().HasMaxLength(100);
            Property(x => x.OriginalCurrency).HasColumnName("OriginalCurrency").IsOptional().HasMaxLength(5);
            Property(x => x.OriginalAmount).HasColumnName("OriginalAmount").IsOptional();
            Property(x => x.Euros).HasColumnName("Euros").IsOptional();
            Property(x => x.EurosNet).HasColumnName("EurosNet").IsOptional();
            Property(x => x.KudosValue).HasColumnName("KudosValue").IsRequired();
            Property(x => x.ItemName).HasColumnName("ItemName").IsOptional().HasMaxLength(50);
            Property(x => x.ItemCode).HasColumnName("ItemCode").IsOptional().HasMaxLength(50);
            Property(x => x.Email).HasColumnName("Email").IsOptional().HasMaxLength(50);
            Property(x => x.Comment).HasColumnName("Comment").IsOptional().HasMaxLength(200);
            Property(x => x.PackID).HasColumnName("PackID").IsOptional();
            Property(x => x.RedeemCode).HasColumnName("RedeemCode").IsOptional().HasMaxLength(100);
            Property(x => x.IsSpringContribution).HasColumnName("IsSpringContribution").IsRequired();
            Property(x => x.ManuallyAddedAccountID).HasColumnName("ManuallyAddedAccountID").IsOptional();
            Property(x => x.ContributionJarID).HasColumnName("ContributionJarID").IsOptional();

            // Foreign keys
            HasOptional(a => a.Account_AccountID).WithMany(b => b.Contributions_AccountID).HasForeignKey(c => c.AccountID); // FK_Contribution_Account
            HasOptional(a => a.Account_ManuallyAddedAccountID).WithMany(b => b.Contributions_ManuallyAddedAccountID).HasForeignKey(c => c.ManuallyAddedAccountID); // FK_Contribution_Account1
            HasOptional(a => a.ContributionJar).WithMany(b => b.Contributions).HasForeignKey(c => c.ContributionJarID); // FK_Contribution_ContributionJar
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
