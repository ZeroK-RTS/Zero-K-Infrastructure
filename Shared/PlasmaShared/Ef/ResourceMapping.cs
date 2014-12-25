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
    // Resource
    internal partial class ResourceMapping : EntityTypeConfiguration<Resource>
    {
        public ResourceMapping(string schema = "dbo")
        {
            ToTable(schema + ".Resource");
            HasKey(x => x.ResourceID);

            Property(x => x.ResourceID).HasColumnName("ResourceID").IsRequired().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(x => x.InternalName).HasColumnName("InternalName").IsRequired().HasMaxLength(255);
            Property(x => x.TypeID).HasColumnName("TypeID").IsRequired();
            Property(x => x.LastLinkCheck).HasColumnName("LastLinkCheck").IsOptional();
            Property(x => x.DownloadCount).HasColumnName("DownloadCount").IsRequired();
            Property(x => x.NoLinkDownloadCount).HasColumnName("NoLinkDownloadCount").IsRequired();
            Property(x => x.MissionID).HasColumnName("MissionID").IsOptional();
            Property(x => x.LastChange).HasColumnName("LastChange").IsOptional();
            Property(x => x.AuthorName).HasColumnName("AuthorName").IsOptional().HasMaxLength(200);
            Property(x => x.MapTags).HasColumnName("MapTags").IsOptional();
            Property(x => x.MapWidth).HasColumnName("MapWidth").IsOptional();
            Property(x => x.MapHeight).HasColumnName("MapHeight").IsOptional();
            Property(x => x.MapSizeSquared).HasColumnName("MapSizeSquared").IsOptional();
            Property(x => x.MapSizeRatio).HasColumnName("MapSizeRatio").IsOptional();
            Property(x => x.MapIsSupported).HasColumnName("MapIsSupported").IsOptional();
            Property(x => x.MapIsAssymetrical).HasColumnName("MapIsAssymetrical").IsOptional();
            Property(x => x.MapHills).HasColumnName("MapHills").IsOptional();
            Property(x => x.MapWaterLevel).HasColumnName("MapWaterLevel").IsOptional();
            Property(x => x.MapIs1v1).HasColumnName("MapIs1v1").IsOptional();
            Property(x => x.MapIsTeams).HasColumnName("MapIsTeams").IsOptional();
            Property(x => x.MapIsFfa).HasColumnName("MapIsFfa").IsOptional();
            Property(x => x.MapIsChickens).HasColumnName("MapIsChickens").IsOptional();
            Property(x => x.MapIsSpecial).HasColumnName("MapIsSpecial").IsOptional();
            Property(x => x.MapFFAMaxTeams).HasColumnName("MapFFAMaxTeams").IsOptional();
            Property(x => x.MapRatingCount).HasColumnName("MapRatingCount").IsOptional();
            Property(x => x.MapRatingSum).HasColumnName("MapRatingSum").IsOptional();
            Property(x => x.TaggedByAccountID).HasColumnName("TaggedByAccountID").IsOptional();
            Property(x => x.ForumThreadID).HasColumnName("ForumThreadID").IsOptional();
            Property(x => x.FeaturedOrder).HasColumnName("FeaturedOrder").IsOptional();
            Property(x => x.MapPlanetWarsIcon).HasColumnName("MapPlanetWarsIcon").IsOptional().HasMaxLength(50);
            Property(x => x.RatingPollID).HasColumnName("RatingPollID").IsOptional();
            Property(x => x.MapSpringieCommands).HasColumnName("MapSpringieCommands").IsOptional().HasMaxLength(2000);

            // Foreign keys
            HasOptional(a => a.Mission).WithMany(b => b.Resources).HasForeignKey(c => c.MissionID); // FK_Resource_Mission
            HasOptional(a => a.Account).WithMany(b => b.Resources).HasForeignKey(c => c.TaggedByAccountID); // FK_Resource_Account
            HasOptional(a => a.ForumThread).WithMany(b => b.Resources).HasForeignKey(c => c.ForumThreadID); // FK_Resource_ForumThread
            HasOptional(a => a.RatingPoll).WithMany(b => b.Resources).HasForeignKey(c => c.RatingPollID); // FK_Resource_RatingPoll
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
