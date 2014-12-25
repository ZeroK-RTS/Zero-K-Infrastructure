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
    // RatingPoll
    internal partial class RatingPollMapping : EntityTypeConfiguration<RatingPoll>
    {
        public RatingPollMapping(string schema = "dbo")
        {
            ToTable(schema + ".RatingPoll");
            HasKey(x => x.RatingPollID);

            Property(x => x.RatingPollID).HasColumnName("RatingPollID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.Average).HasColumnName("Average").IsOptional();
            Property(x => x.Count).HasColumnName("Count").IsRequired();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
