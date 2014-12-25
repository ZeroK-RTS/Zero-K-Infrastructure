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
    // AbuseReport
    internal partial class AbuseReportMapping : EntityTypeConfiguration<AbuseReport>
    {
        public AbuseReportMapping(string schema = "dbo")
        {
            ToTable(schema + ".AbuseReport");
            HasKey(x => x.AbuseReportID);

            Property(x => x.AbuseReportID).HasColumnName("AbuseReportID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired();
            Property(x => x.ReporterAccountID).HasColumnName("ReporterAccountID").IsRequired();
            Property(x => x.Time).HasColumnName("Time").IsRequired();
            Property(x => x.Text).HasColumnName("Text").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account_AccountID).WithMany(b => b.AbuseReportsByAccountID).HasForeignKey(c => c.AccountID); // FK_AbuseReport_Account
            HasRequired(a => a.AccountByReporterAccountID).WithMany(b => b.AbuseReports_ReporterAccountID).HasForeignKey(c => c.ReporterAccountID); // FK_AbuseReport_Account1
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
