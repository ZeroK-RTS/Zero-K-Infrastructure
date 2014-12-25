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
    // Avatar
    internal partial class AvatarMapping : EntityTypeConfiguration<Avatar>
    {
        public AvatarMapping(string schema = "dbo")
        {
            ToTable(schema + ".Avatar");
            HasKey(x => x.AvatarName);

            Property(x => x.AvatarName).HasColumnName("AvatarName").IsRequired().HasMaxLength(50).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
