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
    // MiscVar
    internal partial class MiscVarMapping : EntityTypeConfiguration<MiscVar>
    {
        public MiscVarMapping(string schema = "dbo")
        {
            ToTable(schema + ".MiscVar");
            HasKey(x => x.VarName);

            Property(x => x.VarName).HasColumnName("VarName").IsRequired();
            Property(x => x.VarValue).HasColumnName("VarValue").IsOptional();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
