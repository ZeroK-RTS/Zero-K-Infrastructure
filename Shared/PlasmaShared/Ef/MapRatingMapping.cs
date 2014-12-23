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
    // MapRating
    internal partial class MapRatingMapping : EntityTypeConfiguration<MapRating>
    {
        public MapRatingMapping(string schema = "dbo")
        {
            ToTable(schema + ".MapRating");
            HasKey(x => new { x.ResourceID, x.AccountID });

            Property(x => x.ResourceID).HasColumnName("ResourceID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Rating).HasColumnName("Rating").IsRequired();

            // Foreign keys
            HasRequired(a => a.Resource).WithMany(b => b.MapRatings).HasForeignKey(c => c.ResourceID); // FK_ResourceRating_Resource
            HasRequired(a => a.Account).WithMany(b => b.MapRatings).HasForeignKey(c => c.AccountID); // FK_ResourceRating_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
