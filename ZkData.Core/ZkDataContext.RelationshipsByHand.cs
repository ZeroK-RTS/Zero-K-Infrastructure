using Microsoft.EntityFrameworkCore;

namespace ZkData
{
    /// <summary>
    /// The relationships the generator refuses to guess at - see
    /// ZkData.Core/generate-relationships.py, which lists them in its output so the two
    /// files can be checked against each other.
    ///
    /// Kept apart from the generated file so regenerating cannot lose them.
    /// </summary>
    public partial class ZkDataContext
    {
        partial void ConfigureRelationshipsByHand(ModelBuilder modelBuilder)
        {

            // EF6: HasRequired(e => e.Resource) with no inverse - a required one-way
            // reference. EF Core needs the inverse spelled, even as none.
            // The foreign key is BannedMapResourceID, not the ResourceID EF Core would
            // invent from the navigation's type name. Naming it is what stops a shadow
            // column and a second index appearing.
            // Both ends have to be named. With WithMany() and no inverse, Resource's own
            // BansByAccountID collection stays unpaired and EF Core builds a SECOND
            // relationship for it, with a shadow ResourceID column and its own index -
            // which is exactly what the schema diff showed.
            modelBuilder.Entity<AccountMapBan>().HasOne(e => e.Resource)
                .WithMany(r => r.BansByAccountID)
                .HasForeignKey(e => e.BannedMapResourceID)
                .IsRequired(true).OnDelete(DeleteBehavior.Cascade);

            // EF6: HasOptional(...).WithRequired(...) is a one-to-one where the dependent
            // shares the principal's key. EF Core states that as HasOne/WithOne with the
            // dependent named.
            modelBuilder.Entity<Unlock>().HasOne(e => e.CommanderDecorationIcon)
                .WithOne(e => e.Unlock)
                .HasForeignKey<CommanderDecorationIcon>(e => e.DecorationUnlockID)
                .IsRequired(false).OnDelete(DeleteBehavior.Restrict);

            // LobbyChatHistory is clustered on Time, not on its key. That is a deliberate
            // choice in the database - the table is written in time order and read in time
            // ranges - and EF Core would cluster the primary key instead, which changes the
            // physical order of the largest chat table.
            modelBuilder.Entity<LobbyChatHistory>().HasKey(e => e.LobbyChatHistoryID).IsClustered(false);
            modelBuilder.Entity<LobbyChatHistory>().HasIndex(e => e.Time).IsClustered();

            // Two relationships where EF6 inferred the foreign key column and EF Core infers
            // a different name. EF6 used the principal's key name as it stands - EffectTypeID,
            // OptionID - while EF Core prefixes the navigation name, giving
            // TreatyEffectTypeEffectTypeID and PollOptionOptionID, and then creates those as
            // shadow columns because the real ones are already there.
            //
            // Only two of the 35 such statements differ; the rest happen to agree. The
            // column names come from db/schema/schema.txt.
            modelBuilder.Entity<TreatyEffectType>().HasMany(e => e.TreatyEffects)
                .WithOne(e => e.TreatyEffectType)
                .HasForeignKey(e => e.EffectTypeID)
                .IsRequired(true).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PollOption>().HasMany(e => e.PollVotes)
                .WithOne(e => e.PollOption)
                .HasForeignKey(e => e.OptionID)
                .IsRequired(true).OnDelete(DeleteBehavior.Restrict);

            // The three relationships into CampaignPlanets that EF6 left without a key.
            //
            // CampaignPlanets is keyed on (CampaignID, PlanetID), and EF6 matched that
            // against the dependent's identically named properties by convention. EF Core's
            // convention is stricter: it will not use a property whose nullability disagrees
            // with the relationship, so on CampaignJournals - optional, with a nullable
            // PlanetID and a NOT NULL CampaignID - it declined the match and invented
            // CampaignPlanetCampaignID and CampaignPlanetPlanetID instead.
            //
            // Shadow foreign keys are the quiet failure in this port. The model builds, it
            // validates, and every query against that table fails at runtime on a column the
            // database has never heard of. The other two resolved correctly by convention;
            // they are written out anyway, because "correct by convention" is what the third
            // one also looked like until something read from it.
            modelBuilder.Entity<CampaignPlanet>().HasMany(e => e.AccountCampaignProgress)
                .WithOne(e => e.CampaignPlanet)
                .HasForeignKey(e => new { e.CampaignID, e.PlanetID })
                .IsRequired(true).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CampaignPlanet>().HasMany(e => e.CampaignJournals)
                .WithOne(e => e.CampaignPlanet)
                .HasForeignKey(e => new { e.CampaignID, e.PlanetID })
                .IsRequired(false).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CampaignPlanet>().HasMany(e => e.CampaignPlanetVars)
                .WithOne(e => e.CampaignPlanet)
                .HasForeignKey(e => new { e.CampaignID, e.PlanetID })
                .IsRequired(true).OnDelete(DeleteBehavior.Restrict);

            // CampaignEvent sits on two relationships that share the CampaignID column: one
            // to Campaign on CampaignID alone, and one to CampaignPlanet on
            // (CampaignID, PlanetID). EF6 accepted the overlap. EF Core resolves it by
            // inventing shadow columns for the composite one unless it is stated last and
            // in full - which is what this does. Declared here rather than in the generated
            // file because it has to run after everything the generator emits.
            modelBuilder.Entity<CampaignEvent>().HasOne(e => e.CampaignPlanet)
                .WithMany(e => e.CampaignEvents)
                .HasForeignKey(e => new { e.CampaignID, e.PlanetID })
                .IsRequired(false).OnDelete(DeleteBehavior.Cascade);

            // The five many-to-many joins. EF6 described the join table with
            // Map/MapLeftKey/MapRightKey; EF Core uses UsingEntity. Table and column names
            // are given explicitly so the schema keeps the names the database already has.
            //
            // The third lambda configures the join entity itself, and both parts of it
            // matter. The key's column ORDER is not cosmetic - it is the clustered index's
            // order, so EventAccount really is keyed (EventID, AccountID) while EventClan
            // is (ClanID, EventID). And EF Core indexes only the second key column, the
            // first being the key's own prefix; EF6 indexed both, so the missing one is
            // declared here.

            modelBuilder.Entity<Clan>().HasMany(e => e.Events).WithMany(e => e.Clans)
                .UsingEntity("EventClan",
                    l => l.HasOne(typeof(Event)).WithMany().HasForeignKey("EventID"),
                    r => r.HasOne(typeof(Clan)).WithMany().HasForeignKey("ClanID"),
                    j =>
                    {
                        j.HasKey("ClanID", "EventID");
                        j.HasIndex("ClanID");
                        j.HasIndex("EventID");
                    });

            modelBuilder.Entity<Event>().HasMany(e => e.Accounts).WithMany(e => e.Events)
                .UsingEntity("EventAccount",
                    l => l.HasOne(typeof(Account)).WithMany().HasForeignKey("AccountID"),
                    r => r.HasOne(typeof(Event)).WithMany().HasForeignKey("EventID"),
                    j =>
                    {
                        j.HasKey("EventID", "AccountID");
                        j.HasIndex("AccountID");
                        j.HasIndex("EventID");
                    });

            modelBuilder.Entity<Event>().HasMany(e => e.Factions).WithMany(e => e.Events)
                .UsingEntity("EventFaction",
                    l => l.HasOne(typeof(Faction)).WithMany().HasForeignKey("FactionID"),
                    r => r.HasOne(typeof(Event)).WithMany().HasForeignKey("EventID"),
                    j =>
                    {
                        j.HasKey("EventID", "FactionID");
                        j.HasIndex("EventID");
                        j.HasIndex("FactionID");
                    });

            modelBuilder.Entity<Event>().HasMany(e => e.Planets).WithMany(e => e.Events)
                .UsingEntity("EventPlanet",
                    l => l.HasOne(typeof(Planet)).WithMany().HasForeignKey("PlanetID"),
                    r => r.HasOne(typeof(Event)).WithMany().HasForeignKey("EventID"),
                    j =>
                    {
                        j.HasKey("EventID", "PlanetID");
                        j.HasIndex("EventID");
                        j.HasIndex("PlanetID");
                    });

            modelBuilder.Entity<Event>().HasMany(e => e.SpringBattles).WithMany(e => e.Events)
                .UsingEntity("EventSpringBattle",
                    l => l.HasOne(typeof(SpringBattle)).WithMany().HasForeignKey("SpringBattleID"),
                    r => r.HasOne(typeof(Event)).WithMany().HasForeignKey("EventID"),
                    j =>
                    {
                        j.HasKey("EventID", "SpringBattleID");
                        j.HasIndex("EventID");
                        j.HasIndex("SpringBattleID");
                    });
        }
    }
}
