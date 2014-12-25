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
    // BlockedCompany
    internal partial class BlockedCompanyMapping : EntityTypeConfiguration<BlockedCompany>
    {
        public BlockedCompanyMapping(string schema = "dbo")
        {
            ToTable(schema + ".BlockedCompany");
            HasKey(x => x.CompanyID);

            Property(x => x.CompanyID).HasColumnName("CompanyID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.CompanyName).HasColumnName("CompanyName").IsRequired();
            Property(x => x.Comment).HasColumnName("Comment").IsOptional();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
