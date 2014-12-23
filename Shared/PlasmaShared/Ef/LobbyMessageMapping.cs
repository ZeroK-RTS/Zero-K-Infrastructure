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
    // LobbyMessage
    internal partial class LobbyMessageMapping : EntityTypeConfiguration<LobbyMessage>
    {
        public LobbyMessageMapping(string schema = "dbo")
        {
            ToTable(schema + ".LobbyMessage");
            HasKey(x => x.MessageID);

            Property(x => x.MessageID).HasColumnName("MessageID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.SourceName).HasColumnName("SourceName").IsRequired().HasMaxLength(200);
            Property(x => x.TargetName).HasColumnName("TargetName").IsRequired().HasMaxLength(200);
            Property(x => x.SourceLobbyID).HasColumnName("SourceLobbyID").IsOptional();
            Property(x => x.Message).HasColumnName("Message").IsOptional().HasMaxLength(2000);
            Property(x => x.Created).HasColumnName("Created").IsRequired();
            Property(x => x.TargetLobbyID).HasColumnName("TargetLobbyID").IsOptional();
            Property(x => x.Channel).HasColumnName("Channel").IsOptional().HasMaxLength(100);
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
