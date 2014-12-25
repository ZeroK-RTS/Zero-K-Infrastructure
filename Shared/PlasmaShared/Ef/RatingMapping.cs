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
    // Rating
    internal partial class RatingMapping : EntityTypeConfiguration<Rating>
    {
        public RatingMapping(string schema = "dbo")
        {
            ToTable(schema + ".Rating");
            HasKey(x => x.RatingID);

            Property(x => x.RatingID).HasColumnName("RatingID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired();
            Property(x => x.MissionID).HasColumnName("MissionID").IsOptional();
            Property(x => x.Rating_).HasColumnName("Rating").IsOptional();
            Property(x => x.Difficulty).HasColumnName("Difficulty").IsOptional();

            // Foreign keys
            HasRequired(a => a.Account).WithMany(b => b.Ratings).HasForeignKey(c => c.AccountID); // FK_Rating_Account
            HasOptional(a => a.Mission).WithMany(b => b.Ratings).HasForeignKey(c => c.MissionID); // FK_Rating_Mission
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
