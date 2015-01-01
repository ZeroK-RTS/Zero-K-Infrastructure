using PlasmaShared.Migrations;

namespace ZkData
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class ZkDataContext : DbContext
    {
        private static string ConnectionStringLocal = @"Data Source=.\SQLEXPRESS;Initial Catalog=zero-k_ef;Integrated Security=True;MultipleActiveResultSets=true";

        private static string ConnectionStringLive = @"Data Source=omega.licho.eu,100;Initial Catalog=zero-k_ef;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true";



        public virtual DbSet<AbuseReport> AbuseReports { get; set; }
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<AccountBattleAward> AccountBattleAwards { get; set; }
        public virtual DbSet<AccountCampaignJournalProgress> AccountCampaignJournalProgress { get; set; }
        public virtual DbSet<AccountCampaignProgress> AccountCampaignProgress { get; set; }
        public virtual DbSet<AccountCampaignVar> AccountCampaignVars { get; set; }
        public virtual DbSet<AccountForumVote> AccountForumVotes { get; set; }
        public virtual DbSet<AccountIP> AccountIPs { get; set; }
        public virtual DbSet<AccountPlanet> AccountPlanets { get; set; }
        public virtual DbSet<AccountRole> AccountRoles { get; set; }
        public virtual DbSet<AccountUnlock> AccountUnlocks { get; set; }
        public virtual DbSet<AccountUserID> AccountUserIDs { get; set; }
        public virtual DbSet<AutohostConfig> AutohostConfigs { get; set; }
        public virtual DbSet<Avatar> Avatars { get; set; }
        public virtual DbSet<BlockedCompany> BlockedCompanies { get; set; }
        public virtual DbSet<BlockedHost> BlockedHosts { get; set; }
        public virtual DbSet<Campaign> Campaigns { get; set; }
        public virtual DbSet<CampaignEvent> CampaignEvents { get; set; }
        public virtual DbSet<CampaignJournal> CampaignJournals { get; set; }
        public virtual DbSet<CampaignJournalVar> CampaignJournalVars { get; set; }
        public virtual DbSet<CampaignLink> CampaignLinks { get; set; }
        public virtual DbSet<CampaignPlanet> CampaignPlanets { get; set; }
        public virtual DbSet<CampaignPlanetVar> CampaignPlanetVars { get; set; }
        public virtual DbSet<CampaignVar> CampaignVars { get; set; }
        public virtual DbSet<Clan> Clans { get; set; }
        public virtual DbSet<Commander> Commanders { get; set; }
        public virtual DbSet<CommanderDecoration> CommanderDecorations { get; set; }
        public virtual DbSet<CommanderDecorationIcon> CommanderDecorationIcons { get; set; }
        public virtual DbSet<CommanderDecorationSlot> CommanderDecorationSlots { get; set; }
        public virtual DbSet<CommanderModule> CommanderModules { get; set; }
        public virtual DbSet<CommanderSlot> CommanderSlots { get; set; }
        public virtual DbSet<Contribution> Contributions { get; set; }
        public virtual DbSet<ContributionJar> ContributionJars { get; set; }
        public virtual DbSet<Event> Events { get; set; }
        public virtual DbSet<Faction> Factions { get; set; }
        public virtual DbSet<FactionTreaty> FactionTreaties { get; set; }
        public virtual DbSet<ForumCategory> ForumCategories { get; set; }
        public virtual DbSet<ForumLastRead> ForumLastReads { get; set; }
        public virtual DbSet<ForumPost> ForumPosts { get; set; }
        public virtual DbSet<ForumPostEdit> ForumPostEdits { get; set; }
        public virtual DbSet<ForumThread> ForumThreads { get; set; }
        public virtual DbSet<ForumThreadLastRead> ForumThreadLastReads { get; set; }
        public virtual DbSet<Galaxy> Galaxies { get; set; }
        public virtual DbSet<KudosPurchase> KudosPurchases { get; set; }
        public virtual DbSet<Link> Links { get; set; }
        public virtual DbSet<LobbyChannelSubscription> LobbyChannelSubscriptions { get; set; }
        public virtual DbSet<LobbyMessage> LobbyMessages { get; set; }
        public virtual DbSet<MapRating> MapRatings { get; set; }
        public virtual DbSet<Mission> Missions { get; set; }
        public virtual DbSet<MissionScore> MissionScores { get; set; }
        public virtual DbSet<News> News { get; set; }
        public virtual DbSet<Planet> Planets { get; set; }
        public virtual DbSet<PlanetFaction> PlanetFactions { get; set; }
        public virtual DbSet<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }
        public virtual DbSet<PlanetStructure> PlanetStructures { get; set; }
        public virtual DbSet<Poll> Polls { get; set; }
        public virtual DbSet<PollOption> PollOptions { get; set; }
        public virtual DbSet<PollVote> PollVotes { get; set; }
        public virtual DbSet<Punishment> Punishments { get; set; }
        public virtual DbSet<Rating> Ratings { get; set; }
        public virtual DbSet<Resource> Resources { get; set; }
        public virtual DbSet<ResourceContentFile> ResourceContentFiles { get; set; }
        public virtual DbSet<ResourceDependency> ResourceDependencies { get; set; }
        public virtual DbSet<ResourceSpringHash> ResourceSpringHashes { get; set; }
        public virtual DbSet<RoleType> RoleTypes { get; set; }
        public virtual DbSet<RoleTypeHierarchy> RoleTypeHierarchies { get; set; }
        public virtual DbSet<SpringBattle> SpringBattles { get; set; }
        public virtual DbSet<SpringBattlePlayer> SpringBattlePlayers { get; set; }
        public virtual DbSet<StructureType> StructureTypes { get; set; }
        public virtual DbSet<TreatyEffect> TreatyEffects { get; set; }
        public virtual DbSet<TreatyEffectType> TreatyEffectTypes { get; set; }
        public virtual DbSet<Unlock> Unlocks { get; set; }
        public virtual DbSet<MiscVar> MiscVars { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<Account>()
                .Property(e => e.Aliases)
                .IsUnicode(false);

            modelBuilder.Entity<Account>()
                .Property(e => e.Password)
                .IsUnicode(false);

            modelBuilder.Entity<Account>()
                .Property(e => e.Country)
                .IsUnicode(false);

            modelBuilder.Entity<Account>()
                .Property(e => e.SteamID)
                .HasPrecision(38, 0);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.AbuseReportsByAccountID)
                .WithRequired(e => e.AccountByAccountID)
                .HasForeignKey(e => e.AccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.AbuseReportsByReporterAccountID)
                .WithRequired(e => e.AccountByReporterAccountID)
                .HasForeignKey(e => e.ReporterAccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.AccountBattleAwards)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.AccountCampaignJournalProgresses)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.AccountForumVotes)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);


            modelBuilder.Entity<Account>()
                .HasMany(e => e.AccountRolesByAccountID)
                .WithRequired(e => e.Account)
                .HasForeignKey(e=>e.AccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.AccountUnlocks)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.CampaignEvents)
                .WithRequired(e => e.Account)
                .HasForeignKey(e=>e.AccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.Commanders)
                .WithRequired(e => e.AccountByAccountID)
                .HasForeignKey(e=>e.AccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.ContributionsByAccountID)
                .WithOptional(e => e.AccountByAccountID)
                .HasForeignKey(e => e.AccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.ContributionsByManuallyAddedAccountID)
                .WithOptional(e => e.AccountByManuallyAddedID)
                .HasForeignKey(e => e.ManuallyAddedAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.ContributionJars)
                .WithRequired(e => e.Account)
                .HasForeignKey(e => e.GuarantorAccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.FactionTreatiesByProposingAccount)
                .WithRequired(e => e.AccountByProposingAccountID)
                .HasForeignKey(e => e.ProposingAccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.FactionTreatiesByAcceptedAccountID)
                .WithOptional(e => e.AccountByAcceptedAccountID)
                .HasForeignKey(e => e.AcceptedAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.ForumLastReads)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.ForumPostEdits)
                .WithRequired(e => e.Account)
                .HasForeignKey(e => e.EditorAccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.ForumThreads)
                .WithOptional(e => e.AccountByCreatedAccountID)
                .HasForeignKey(e => e.CreatedAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.ForumThreadsByLastPostAccountID)
                .WithOptional(e => e.AccountByLastPostAccountID)
                .HasForeignKey(e => e.LastPostAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.ForumThreadLastReads)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.KudosPurchases)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.Missions)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.MissionScores)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.News)
                .WithRequired(e => e.Account)
                .HasForeignKey(e => e.AuthorAccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.Planets)
                .WithOptional(e => e.Account)
                .HasForeignKey(e => e.OwnerAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.PlanetOwnerHistories)
                .WithOptional(e => e.Account)
                .HasForeignKey(e => e.OwnerAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.PlanetStructures)
                .WithOptional(e => e.Account)
                .HasForeignKey(e => e.OwnerAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.AccountPlanets)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.PollsByRoleTargetAccountID)
                .WithOptional(e => e.AccountByRoleTargetAccountID)
                .HasForeignKey(e => e.RoleTargetAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.PollsByCreatedAccountID)
                .WithOptional(e => e.CreatedAccount)
                .HasForeignKey(e => e.CreatedAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.PollVotes)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.PunishmentsByAccountID)
                .WithRequired(e => e.AccountByAccountID)
                .HasForeignKey(e => e.AccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.PunishmentsByCreatedAccountID)
                .WithOptional(e => e.AccountByCreatedAccountID)
                .HasForeignKey(e => e.CreatedAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.Ratings)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.Resources)
                .WithOptional(e => e.Account)
                .HasForeignKey(e => e.TaggedByAccountID);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.MapRatings)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.SpringBattles)
                .WithRequired(e => e.Account)
                .HasForeignKey(e => e.HostAccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e => e.SpringBattlePlayers)
                .WithRequired(e => e.Account)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasOptional(e=>e.Clan)
                .WithMany(e=>e.Accounts)
                .HasForeignKey(e=>e.ClanID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Account>()
                .HasMany(e=>e.ForumPosts)
                .WithRequired(e=>e.Account)
                .HasForeignKey(e=>e.AuthorAccountID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AccountBattleAward>()
                .Property(e => e.AwardKey)
                .IsUnicode(false);

            modelBuilder.Entity<Campaign>()
                .HasMany(e => e.AccountCampaignJournalProgresses)
                .WithRequired(e => e.Campaign)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Campaign>()
                .HasMany(e => e.AccountCampaignProgresses)
                .WithRequired(e => e.Campaign)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Campaign>()
                .HasMany(e => e.AccountCampaignVars)
                .WithRequired(e => e.Campaign)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Campaign>()
                .HasMany(e => e.CampaignEvents)
                .WithRequired(e => e.Campaign)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Campaign>()
                .HasMany(e => e.CampaignJournalVars)
                .WithRequired(e => e.Campaign)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Campaign>()
                .HasMany(e => e.CampaignLinks)
                .WithRequired(e => e.Campaign)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Campaign>()
                .HasMany(e => e.CampaignPlanetVars)
                .WithRequired(e => e.Campaign)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Campaign>()
                .HasMany(e => e.CampaignVars)
                .WithRequired(e => e.Campaign)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CampaignJournal>()
                .HasMany(e => e.AccountCampaignJournalProgress)
                .WithRequired(e => e.CampaignJournal)
                .HasForeignKey(e => new { e.CampaignID, e.JournalID });

            modelBuilder.Entity<CampaignJournal>()
                .HasMany(e => e.CampaignJournalVars)
                .WithRequired(e => e.CampaignJournal)
                .HasForeignKey(e => new { e.CampaignID, e.JournalID });


            modelBuilder.Entity<CampaignPlanet>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<CampaignPlanet>()
                .HasMany(e => e.AccountCampaignProgress)
                .WithRequired(e => e.CampaignPlanet)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CampaignPlanet>()
                .HasMany(e => e.CampaignEvents)
                .WithOptional(e => e.CampaignPlanet)
                .HasForeignKey(e => new { e.PlanetID, e.CampaignID })
                .WillCascadeOnDelete();

            modelBuilder.Entity<CampaignPlanet>().HasMany(e => e.CampaignJournals).WithOptional(e => e.CampaignPlanet);

            modelBuilder.Entity<CampaignPlanet>()
                .HasMany(e => e.CampaignLinksByPlanetToUnlock)
                .WithRequired(e => e.PlanetToUnlock)
                .HasForeignKey(e => new { e.PlanetToUnlockID, e.CampaignID })
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CampaignPlanet>()
                .HasMany(e => e.CampaignLinksByUnlockingPlanet)
                .WithRequired(e => e.UnlockingPlanet)
                .HasForeignKey(e => new { e.UnlockingPlanetID, e.CampaignID })
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CampaignPlanet>()
                .HasMany(e => e.CampaignPlanetVars)
                .WithRequired(e => e.CampaignPlanet)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CampaignVar>()
                .HasMany(e => e.AccountCampaignVars)
                .WithRequired(e => e.CampaignVar)
                .HasForeignKey(e => new { e.CampaignID, e.VarID })
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CampaignVar>()
                .HasMany(e => e.CampaignJournalVars)
                .WithRequired(e => e.CampaignVar)
                .HasForeignKey(e => new { e.CampaignID, e.RequiredVarID })
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CampaignVar>()
                .HasMany(e => e.CampaignPlanetVars)
                .WithRequired(e => e.CampaignVar)
                .HasForeignKey(e => new { e.CampaignID, e.RequiredVarID })
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Clan>()
                .Property(e => e.ClanName)
                .IsUnicode(false);

            modelBuilder.Entity<Clan>()
                .Property(e => e.Description)
                .IsUnicode(false);

            modelBuilder.Entity<Clan>()
                .Property(e => e.Password)
                .IsUnicode(false);

            modelBuilder.Entity<Clan>()
                .Property(e => e.Shortcut)
                .IsUnicode(false);

            modelBuilder.Entity<Clan>()
                .HasMany(e => e.AccountRoles)
                .WithOptional(e => e.Clan)
                .WillCascadeOnDelete();

            modelBuilder.Entity<Clan>()
                .HasMany(e => e.ForumThreads)
                .WithOptional(e => e.Clan)
                .HasForeignKey(e => e.RestrictedClanID)
                .WillCascadeOnDelete();

            modelBuilder.Entity<Clan>()
                .HasMany(e => e.PlanetOwnerHistories)
                .WithOptional(e => e.Clan)
                .HasForeignKey(e => e.OwnerClanID);

            modelBuilder.Entity<Clan>()
                .HasMany(e => e.Events)
                .WithMany(e => e.Clans)
                .Map(m => m.ToTable("EventClan").MapLeftKey("ClanID").MapRightKey("EventID"));

            modelBuilder.Entity<Clan>()
                .HasMany(e=>e.Polls)
                .WithOptional(e=>e.Clan)
                .HasForeignKey(e=>e.RestrictClanID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CommanderDecorationSlot>()
                .HasMany(e => e.CommanderDecorations)
                .WithRequired(e => e.CommanderDecorationSlot)
                .HasForeignKey(e => e.SlotID);

            modelBuilder.Entity<CommanderSlot>()
                .HasMany(e => e.CommanderModules)
                .WithRequired(e => e.CommanderSlot)
                .HasForeignKey(e => e.SlotID);

            modelBuilder.Entity<Event>()
                .HasMany(e => e.Accounts)
                .WithMany(e => e.Events)
                .Map(m => m.ToTable("EventAccount").MapLeftKey("EventID").MapRightKey("AccountID"));

            modelBuilder.Entity<Event>()
                .HasMany(e => e.Factions)
                .WithMany(e => e.Events)
                .Map(m => m.ToTable("EventFaction").MapLeftKey("EventID").MapRightKey("FactionID"));

            modelBuilder.Entity<Event>()
                .HasMany(e => e.Planets)
                .WithMany(e => e.Events)
                .Map(m => m.ToTable("EventPlanet").MapLeftKey("EventID").MapRightKey("PlanetID"));

            modelBuilder.Entity<Event>()
                .HasMany(e => e.SpringBattles)
                .WithMany(e => e.Events)
                .Map(m => m.ToTable("EventSpringBattle").MapLeftKey("EventID").MapRightKey("SpringBattleID"));

            modelBuilder.Entity<Faction>()
                .Property(e => e.Color)
                .IsUnicode(false);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.FactionTreatiesByProposingFaction)
                .WithRequired(e => e.FactionByProposingFactionID)
                .HasForeignKey(e => e.ProposingFactionID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.FactionTreatiesByAcceptingFaction)
                .WithRequired(e => e.FactionByAcceptingFactionID)
                .HasForeignKey(e => e.AcceptingFactionID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.Planets)
                .WithOptional(e => e.Faction)
                .HasForeignKey(e => e.OwnerFactionID);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.PlanetFactions)
                .WithRequired(e => e.Faction)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.PlanetOwnerHistories)
                .WithOptional(e => e.Faction)
                .HasForeignKey(e => e.OwnerFactionID);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.Polls)
                .WithOptional(e => e.Faction)
                .HasForeignKey(e => e.RestrictFactionID);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.RoleTypes)
                .WithOptional(e => e.Faction)
                .HasForeignKey(e => e.RestrictFactionID);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.TreatyEffectsByGivingFactionID)
                .WithRequired(e => e.FactionByGivingFactionID)
                .HasForeignKey(e => e.GivingFactionID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Faction>()
                .HasMany(e => e.TreatyEffectsByReceivingFactionID)
                .WithRequired(e => e.FactionByReceivingFactionID)
                .HasForeignKey(e => e.ReceivingFactionID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ForumCategory>()
                .HasMany(e => e.ChildForumCategories)
                .WithOptional(e => e.ParentForumCategory)
                .HasForeignKey(e => e.ParentForumCategoryID);

            modelBuilder.Entity<ForumCategory>()
                .HasMany(e => e.ForumLastReads)
                .WithRequired(e => e.ForumCategory)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ForumCategory>()
                .HasMany(e => e.ForumThreads)
                .WithOptional(e => e.ForumCategory)
                .WillCascadeOnDelete();

            modelBuilder.Entity<ForumThread>()
                .HasMany(e => e.Clans)
                .WithOptional(e => e.ForumThread)
                .HasForeignKey(e => e.ForumThreadID);

            modelBuilder.Entity<ForumThread>()
                .HasMany(e => e.Missions)
                .WithRequired(e => e.ForumThread)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ForumThread>()
                .HasMany(e => e.News)
                .WithRequired(e => e.ForumThread)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ForumThread>()
                .HasMany(e=>e.ForumPosts)
                .WithRequired(e=>e.ForumThread)
                .HasForeignKey(e=>e.ForumThreadID)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Galaxy>()
                .Property(e => e.MatchMakerState)
                .IsUnicode(false);

            modelBuilder.Entity<Mission>()
                .Property(e => e.Description)
                .IsUnicode(false);

            modelBuilder.Entity<Mission>()
                .Property(e => e.DescriptionStory)
                .IsUnicode(false);

            modelBuilder.Entity<Mission>()
                .Property(e => e.TokenCondition)
                .IsUnicode(false);

            modelBuilder.Entity<Mission>()
                .HasMany(e => e.CampaignPlanets)
                .WithRequired(e => e.Mission)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Mission>()
                .HasMany(e => e.Ratings)
                .WithOptional(e => e.Mission)
                .WillCascadeOnDelete();

            modelBuilder.Entity<Mission>()
                .HasMany(e => e.Resources)
                .WithOptional(e => e.Mission)
                .WillCascadeOnDelete();

            modelBuilder.Entity<News>()
                .Property(e => e.Text)
                .IsUnicode(false);

            modelBuilder.Entity<Planet>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<Planet>()
                .HasMany(e => e.AccountPlanets)
                .WithRequired(e => e.Planet)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Planet>()
                .HasMany(e => e.LinksByPlanetID1)
                .WithRequired(e => e.PlanetByPlanetID1)
                .HasForeignKey(e => e.PlanetID1)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Planet>()
                .HasMany(e => e.LinksByPlanetID2)
                .WithRequired(e => e.PlanetByPlanetID2)
                .HasForeignKey(e => e.PlanetID2)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Planet>()
                .HasMany(e => e.PlanetFactions)
                .WithRequired(e => e.Planet)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Planet>()
                .HasMany(e => e.PlanetStructures)
                .WithRequired(e => e.Planet)
                .HasForeignKey(e => e.PlanetID);

            modelBuilder.Entity<Planet>()
                .HasMany(e => e.PlanetStructuresByTargetPlanetID)
                .WithOptional(e => e.PlanetByTargetPlanetID)
                .HasForeignKey(e => e.TargetPlanetID);

            modelBuilder.Entity<PollOption>()
                .HasMany(e => e.PollVotes)
                .WithRequired(e => e.PollOption)
                .WillCascadeOnDelete(false);


            modelBuilder.Entity<Resource>()
                .HasMany(e => e.Planets)
                .WithOptional(e => e.Resource)
                .HasForeignKey(e => e.MapResourceID);

            modelBuilder.Entity<Resource>()
                .HasMany(e => e.SpringBattlesByModID)
                .WithRequired(e => e.ResourceByModResourceID)
                .HasForeignKey(e => e.ModResourceID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Resource>()
                .HasMany(e => e.SpringBattlesByMapResourceID)
                .WithRequired(e => e.ResourceByMapResourceID)
                .HasForeignKey(e => e.MapResourceID)
                .WillCascadeOnDelete(false);


            modelBuilder.Entity<ResourceContentFile>()
                .Property(e => e.Md5)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<ResourceContentFile>()
                .Property(e => e.Links)
                .IsUnicode(false);

            modelBuilder.Entity<ResourceDependency>()
                .Property(e => e.NeedsInternalName)
                .IsUnicode(false);

            modelBuilder.Entity<RoleType>()
                .HasMany(e => e.Polls)
                .WithOptional(e => e.RoleType)
                .WillCascadeOnDelete();

            modelBuilder.Entity<RoleType>()
                .HasMany(e => e.RoleTypeHierarchiesByMasterRoleTypeID)
                .WithRequired(e => e.MasterRoleType)
                .HasForeignKey(e => e.MasterRoleTypeID);

            modelBuilder.Entity<RoleType>()
                .HasMany(e => e.RoleTypeHierarchiesBySlaveRoleTypeID)
                .WithRequired(e => e.SlaveRoleType)
                .HasForeignKey(e => e.SlaveRoleTypeID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SpringBattle>()
                .Property(e => e.EngineGameID)
                .IsUnicode(false);

            modelBuilder.Entity<StructureType>()
                .HasMany(e => e.PlanetStructures)
                .WithRequired(e => e.StructureType)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TreatyEffectType>()
                .HasMany(e => e.TreatyEffects)
                .WithRequired(e => e.TreatyEffectType)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Unlock>()
                .HasMany(e => e.Commanders)
                .WithRequired(e => e.Unlock)
                .HasForeignKey(e => e.ChassisUnlockID);

            modelBuilder.Entity<Unlock>()
                .HasMany(e => e.CommanderDecorations)
                .WithRequired(e => e.Unlock)
                .HasForeignKey(e => e.DecorationUnlockID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Unlock>()
                .HasOptional(e => e.CommanderDecorationIcon)
                .WithRequired(e => e.Unlock);

            modelBuilder.Entity<Unlock>()
                .HasMany(e => e.CommanderModules)
                .WithRequired(e => e.Unlock)
                .HasForeignKey(e => e.ModuleUnlockID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Unlock>()
                .HasMany(e => e.KudosPurchases)
                .WithOptional(e => e.Unlock)
                .WillCascadeOnDelete();

            modelBuilder.Entity<Unlock>()
                .HasMany(e => e.StructureTypes)
                .WithOptional(e => e.Unlock)
                .HasForeignKey(e => e.EffectUnlockID)
                .WillCascadeOnDelete();

            modelBuilder.Entity<Unlock>()
                .HasMany(e => e.ChildUnlocks)
                .WithOptional(e => e.ParentUnlock)
                .HasForeignKey(e => e.RequiredUnlockID);
        }

        static ZkDataContext()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ZkDataContext,Configuration>());
        }

        public ZkDataContext()
            : this(UseLiveDb)
        {

        }

        public void SubmitChanges()
        {
            SubmitAndMergeChanges();
        }

        public void SubmitAndMergeChanges()
        {
            SaveChanges();
            // HACK reimplement this

            /*            try
                        {
                            SubmitChanges(ConflictMode.ContinueOnConflict);
                        }

                        catch (ChangeConflictException)
                        {
                            // Automerge database values for members that client has modified
                            ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);

                            // Submit succeeds on second try.
                            SubmitChanges();
                        }*/

        }


        private static bool wasDbChecked = false;
        private static object locker = new object();

#if DEBUG
        public static bool UseLiveDb = false;
#else 
        public static bool UseLiveDb = true;
#endif

        public static Action<ZkDataContext> DataContextCreated = context => { };
        
        public ZkDataContext(bool? useLiveDb)
            : base(useLiveDb != null ? (useLiveDb.Value ? ConnectionStringLive : ConnectionStringLocal) : (UseLiveDb ? ConnectionStringLive : ConnectionStringLocal))
        {
            if (!wasDbChecked)
            {
                lock (locker)
                {
                    Database.CreateIfNotExists();
                    Database.Initialize(false);
                    wasDbChecked = true;
                }
            }
            DataContextCreated(this);
        }

    }
}
