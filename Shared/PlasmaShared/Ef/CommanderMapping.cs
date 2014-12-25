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
    // Commander
    internal partial class CommanderMapping : EntityTypeConfiguration<Commander>
    {
        public CommanderMapping(string schema = "dbo")
        {
            ToTable(schema + ".Commander");
            HasKey(x => x.CommanderID);

            Property(x => x.CommanderID).HasColumnName("CommanderID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired();
            Property(x => x.ProfileNumber).HasColumnName("ProfileNumber").IsRequired();
            Property(x => x.Name).HasColumnName("Name").IsOptional().HasMaxLength(200);
            Property(x => x.ChassisUnlockID).HasColumnName("ChassisUnlockID").IsRequired();

            // Foreign keys
            HasRequired(a => a.AccountByAccountID).WithMany(b => b.Commanders).HasForeignKey(c => c.AccountID); // FK_Commander_Account
            HasRequired(a => a.Unlock).WithMany(b => b.Commanders).HasForeignKey(c => c.ChassisUnlockID); // FK_Commander_Unlock
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
