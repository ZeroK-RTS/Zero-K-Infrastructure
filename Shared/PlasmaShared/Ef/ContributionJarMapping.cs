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
    // ContributionJar
    internal partial class ContributionJarMapping : EntityTypeConfiguration<ContributionJar>
    {
        public ContributionJarMapping(string schema = "dbo")
        {
            ToTable(schema + ".ContributionJar");
            HasKey(x => x.ContributionJarID);

            Property(x => x.ContributionJarID).HasColumnName("ContributionJarID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Name).HasColumnName("Name").IsRequired().HasMaxLength(100);
            Property(x => x.GuarantorAccountID).HasColumnName("GuarantorAccountID").IsRequired();
            Property(x => x.Description).HasColumnName("Description").IsOptional().HasMaxLength(500);
            Property(x => x.TargetGrossEuros).HasColumnName("TargetGrossEuros").IsRequired();
            Property(x => x.IsDefault).HasColumnName("IsDefault").IsRequired();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.ContributionJars).HasForeignKey(c => c.GuarantorAccountID); // FK_ContributionJar_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
