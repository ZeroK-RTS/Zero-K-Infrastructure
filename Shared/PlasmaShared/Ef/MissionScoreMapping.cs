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
    // MissionScore
    internal partial class MissionScoreMapping : EntityTypeConfiguration<MissionScore>
    {
        public MissionScoreMapping(string schema = "dbo")
        {
            ToTable(schema + ".MissionScore");
            HasKey(x => new { x.MissionID, x.AccountID });

            Property(x => x.MissionID).HasColumnName("MissionID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.AccountID).HasColumnName("AccountID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            Property(x => x.Score).HasColumnName("Score").IsRequired();
            Property(x => x.Time).HasColumnName("Time").IsRequired();
            Property(x => x.MissionRevision).HasColumnName("MissionRevision").IsRequired();
            Property(x => x.GameSeconds).HasColumnName("GameSeconds").IsRequired();

            // Foreign keys
            HasRequired(a => a.Mission).WithMany(b => b.MissionScores).HasForeignKey(c => c.MissionID); // FK_MissionScore_Mission
            HasRequired(a => a.Account).WithMany(b => b.MissionScores).HasForeignKey(c => c.AccountID); // FK_MissionScore_Account
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
