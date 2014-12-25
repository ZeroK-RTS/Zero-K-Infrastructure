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
    public partial class ZkDataContext : DbContext, IZkDataContext
    {
        public IDbSet<AbuseReport> AbuseReports { get; set; } // AbuseReport
        public IDbSet<Account> Accounts { get; set; } // Account
        public IDbSet<AccountBattleAward> AccountBattleAwards { get; set; } // AccountBattleAward
        public IDbSet<AccountCampaignJournalProgress> AccountCampaignJournalProgress { get; set; } // AccountCampaignJournalProgress
        public IDbSet<AccountCampaignProgress> AccountCampaignProgress { get; set; } // AccountCampaignProgress
        public IDbSet<AccountCampaignVar> AccountCampaignVars { get; set; } // AccountCampaignVar
        public IDbSet<AccountForumVote> AccountForumVotes { get; set; } // AccountForumVote
        public IDbSet<AccountIP> AccountIPS { get; set; } // AccountIP
        public IDbSet<AccountPlanet> AccountPlanets { get; set; } // AccountPlanet
        public IDbSet<AccountRatingVote> AccountRatingVotes { get; set; } // AccountRatingVote
        public IDbSet<AccountRole> AccountRoles { get; set; } // AccountRole
        public IDbSet<AccountUnlock> AccountUnlocks { get; set; } // AccountUnlock
        public IDbSet<AccountUserID> AccountUserIDS { get; set; } // AccountUserID
        public IDbSet<AutoBanSmurfList> AutoBanSmurfLists { get; set; } // AutoBanSmurfList
        public IDbSet<AutohostConfig> AutohostConfigs { get; set; } // AutohostConfig
        public IDbSet<Avatar> Avatars { get; set; } // Avatar
        public IDbSet<BlockedCompany> BlockedCompanies { get; set; } // BlockedCompany
        public IDbSet<BlockedHost> BlockedHosts { get; set; } // BlockedHost
        public IDbSet<Campaign> Campaigns { get; set; } // Campaign
        public IDbSet<CampaignEvent> CampaignEvents { get; set; } // CampaignEvent
        public IDbSet<CampaignJournal> CampaignJournals { get; set; } // CampaignJournal
        public IDbSet<CampaignJournalVar> CampaignJournalVars { get; set; } // CampaignJournalVar
        public IDbSet<CampaignLink> CampaignLinks { get; set; } // CampaignLink
        public IDbSet<CampaignPlanet> CampaignPlanets { get; set; } // CampaignPlanet
        public IDbSet<CampaignPlanetVar> CampaignPlanetVars { get; set; } // CampaignPlanetVar
        public IDbSet<CampaignVar> CampaignVars { get; set; } // CampaignVar
        public IDbSet<Clan> Clans { get; set; } // Clan
        public IDbSet<Commander> Commanders { get; set; } // Commander
        public IDbSet<CommanderDecoration> CommanderDecorations { get; set; } // CommanderDecoration
        public IDbSet<CommanderDecorationIcon> CommanderDecorationIcons { get; set; } // CommanderDecorationIcon
        public IDbSet<CommanderDecorationSlot> CommanderDecorationSlots { get; set; } // CommanderDecorationSlot
        public IDbSet<CommanderModule> CommanderModules { get; set; } // CommanderModule
        public IDbSet<CommanderSlot> CommanderSlots { get; set; } // CommanderSlot
        public IDbSet<Contribution> Contributions { get; set; } // Contribution
        public IDbSet<ContributionJar> ContributionJars { get; set; } // ContributionJar
        public IDbSet<Event> Events { get; set; } // Event
        public IDbSet<ExceptionLog> ExceptionLogs { get; set; } // ExceptionLog
        public IDbSet<Faction> Factions { get; set; } // Faction
        public IDbSet<FactionTreaty> FactionTreaties { get; set; } // FactionTreaty
        public IDbSet<ForumCategory> ForumCategories { get; set; } // ForumCategory
        public IDbSet<ForumLastRead> ForumLastReads { get; set; } // ForumLastRead
        public IDbSet<ForumPost> ForumPosts { get; set; } // ForumPost
        public IDbSet<ForumPostEdit> ForumPostEdits { get; set; } // ForumPostEdit
        public IDbSet<ForumThread> ForumThreads { get; set; } // ForumThread
        public IDbSet<ForumThreadLastRead> ForumThreadLastReads { get; set; } // ForumThreadLastRead
        public IDbSet<Galaxy> Galaxies { get; set; } // Galaxy
        public IDbSet<KudosPurchase> KudosPurchases { get; set; } // KudosPurchase
        public IDbSet<Link> Links { get; set; } // Link
        public IDbSet<LobbyChannelSubscription> LobbyChannelSubscriptions { get; set; } // LobbyChannelSubscription
        public IDbSet<LobbyMessage> LobbyMessages { get; set; } // LobbyMessage
        public IDbSet<MapRating> MapRatings { get; set; } // MapRating
        public IDbSet<MarketOffer> MarketOffers { get; set; } // MarketOffer
        public IDbSet<MiscVar> MiscVars { get; set; } // MiscVar
        public IDbSet<Mission> Missions { get; set; } // Mission
        public IDbSet<MissionScore> MissionScores { get; set; } // MissionScore
        public IDbSet<News> News { get; set; } // News
        public IDbSet<Planet> Planets { get; set; } // Planet
        public IDbSet<PlanetFaction> PlanetFactions { get; set; } // PlanetFaction
        public IDbSet<PlanetOwnerHistory> PlanetOwnerHistories { get; set; } // PlanetOwnerHistory
        public IDbSet<PlanetStructure> PlanetStructures { get; set; } // PlanetStructure
        public IDbSet<Poll> Polls { get; set; } // Poll
        public IDbSet<PollOption> PollOptions { get; set; } // PollOption
        public IDbSet<PollVote> PollVotes { get; set; } // PollVote
        public IDbSet<Punishment> Punishments { get; set; } // Punishment
        public IDbSet<Rating> Ratings { get; set; } // Rating
        public IDbSet<RatingPoll> RatingPolls { get; set; } // RatingPoll
        public IDbSet<Resource> Resources { get; set; } // Resource
        public IDbSet<ResourceContentFile> ResourceContentFiles { get; set; } // ResourceContentFile
        public IDbSet<ResourceDependency> ResourceDependencies { get; set; } // ResourceDependency
        public IDbSet<ResourceSpringHash> ResourceSpringHashes { get; set; } // ResourceSpringHash
        public IDbSet<RoleType> RoleTypes { get; set; } // RoleType
        public IDbSet<RoleTypeHierarchy> RoleTypeHierarchies { get; set; } // RoleTypeHierarchy
        public IDbSet<SpringBattle> SpringBattles { get; set; } // SpringBattle
        public IDbSet<SpringBattlePlayer> SpringBattlePlayers { get; set; } // SpringBattlePlayer
        public IDbSet<StructureType> StructureTypes { get; set; } // StructureType
        public IDbSet<TreatyEffect> TreatyEffects { get; set; } // TreatyEffect
        public IDbSet<TreatyEffectType> TreatyEffectTypes { get; set; } // TreatyEffectType
        public IDbSet<Unlock> Unlocks { get; set; } // Unlock

        static ZkDataContext()
        {
            Database.SetInitializer<ZkDataContext>(null);
        }

        public ZkDataContext()
            : this(UseLiveDb)
        {
            InitializePartial();
        }

        public ZkDataContext(string connectionString)
            : base(connectionString)
        {
            InitializePartial();
        }

        public ZkDataContext(string connectionString, System.Data.Entity.Infrastructure.DbCompiledModel model)
            : base(connectionString, model)
        {
            InitializePartial();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Configurations.Add(new AbuseReportMapping());
            modelBuilder.Configurations.Add(new AccountMapping());
            modelBuilder.Configurations.Add(new AccountBattleAwardMapping());
            modelBuilder.Configurations.Add(new AccountCampaignJournalProgressMapping());
            modelBuilder.Configurations.Add(new AccountCampaignProgressMapping());
            modelBuilder.Configurations.Add(new AccountCampaignVarMapping());
            modelBuilder.Configurations.Add(new AccountForumVoteMapping());
            modelBuilder.Configurations.Add(new AccountIPMapping());
            modelBuilder.Configurations.Add(new AccountPlanetMapping());
            modelBuilder.Configurations.Add(new AccountRatingVoteMapping());
            modelBuilder.Configurations.Add(new AccountRoleMapping());
            modelBuilder.Configurations.Add(new AccountUnlockMapping());
            modelBuilder.Configurations.Add(new AccountUserIDMapping());
            modelBuilder.Configurations.Add(new AutoBanSmurfListMapping());
            modelBuilder.Configurations.Add(new AutohostConfigMapping());
            modelBuilder.Configurations.Add(new AvatarMapping());
            modelBuilder.Configurations.Add(new BlockedCompanyMapping());
            modelBuilder.Configurations.Add(new BlockedHostMapping());
            modelBuilder.Configurations.Add(new CampaignMapping());
            modelBuilder.Configurations.Add(new CampaignEventMapping());
            modelBuilder.Configurations.Add(new CampaignJournalMapping());
            modelBuilder.Configurations.Add(new CampaignJournalVarMapping());
            modelBuilder.Configurations.Add(new CampaignLinkMapping());
            modelBuilder.Configurations.Add(new CampaignPlanetMapping());
            modelBuilder.Configurations.Add(new CampaignPlanetVarMapping());
            modelBuilder.Configurations.Add(new CampaignVarMapping());
            modelBuilder.Configurations.Add(new ClanMapping());
            modelBuilder.Configurations.Add(new CommanderMapping());
            modelBuilder.Configurations.Add(new CommanderDecorationMapping());
            modelBuilder.Configurations.Add(new CommanderDecorationIconMapping());
            modelBuilder.Configurations.Add(new CommanderDecorationSlotMapping());
            modelBuilder.Configurations.Add(new CommanderModuleMapping());
            modelBuilder.Configurations.Add(new CommanderSlotMapping());
            modelBuilder.Configurations.Add(new ContributionMapping());
            modelBuilder.Configurations.Add(new ContributionJarMapping());
            modelBuilder.Configurations.Add(new EventMapping());
            modelBuilder.Configurations.Add(new ExceptionLogMapping());
            modelBuilder.Configurations.Add(new FactionMapping());
            modelBuilder.Configurations.Add(new FactionTreatyMapping());
            modelBuilder.Configurations.Add(new ForumCategoryMapping());
            modelBuilder.Configurations.Add(new ForumLastReadMapping());
            modelBuilder.Configurations.Add(new ForumPostMapping());
            modelBuilder.Configurations.Add(new ForumPostEditMapping());
            modelBuilder.Configurations.Add(new ForumThreadMapping());
            modelBuilder.Configurations.Add(new ForumThreadLastReadMapping());
            modelBuilder.Configurations.Add(new GalaxyMapping());
            modelBuilder.Configurations.Add(new KudosPurchaseMapping());
            modelBuilder.Configurations.Add(new LinkMapping());
            modelBuilder.Configurations.Add(new LobbyChannelSubscriptionMapping());
            modelBuilder.Configurations.Add(new LobbyMessageMapping());
            modelBuilder.Configurations.Add(new MapRatingMapping());
            modelBuilder.Configurations.Add(new MarketOfferMapping());
            modelBuilder.Configurations.Add(new MiscVarMapping());
            modelBuilder.Configurations.Add(new MissionMapping());
            modelBuilder.Configurations.Add(new MissionScoreMapping());
            modelBuilder.Configurations.Add(new NewsMapping());
            modelBuilder.Configurations.Add(new PlanetMapping());
            modelBuilder.Configurations.Add(new PlanetFactionMapping());
            modelBuilder.Configurations.Add(new PlanetOwnerHistoryMapping());
            modelBuilder.Configurations.Add(new PlanetStructureMapping());
            modelBuilder.Configurations.Add(new PollMapping());
            modelBuilder.Configurations.Add(new PollOptionMapping());
            modelBuilder.Configurations.Add(new PollVoteMapping());
            modelBuilder.Configurations.Add(new PunishmentMapping());
            modelBuilder.Configurations.Add(new RatingMapping());
            modelBuilder.Configurations.Add(new RatingPollMapping());
            modelBuilder.Configurations.Add(new ResourceMapping());
            modelBuilder.Configurations.Add(new ResourceContentFileMapping());
            modelBuilder.Configurations.Add(new ResourceDependencyMapping());
            modelBuilder.Configurations.Add(new ResourceSpringHashMapping());
            modelBuilder.Configurations.Add(new RoleTypeMapping());
            modelBuilder.Configurations.Add(new RoleTypeHierarchyMapping());
            modelBuilder.Configurations.Add(new SpringBattleMapping());
            modelBuilder.Configurations.Add(new SpringBattlePlayerMapping());
            modelBuilder.Configurations.Add(new StructureTypeMapping());
            modelBuilder.Configurations.Add(new TreatyEffectMapping());
            modelBuilder.Configurations.Add(new TreatyEffectTypeMapping());
            modelBuilder.Configurations.Add(new UnlockMapping());
            OnModelCreatingPartial(modelBuilder);
        }

        public static DbModelBuilder CreateModel(DbModelBuilder modelBuilder, string schema)
        {
            modelBuilder.Configurations.Add(new AbuseReportMapping(schema));
            modelBuilder.Configurations.Add(new AccountMapping(schema));
            modelBuilder.Configurations.Add(new AccountBattleAwardMapping(schema));
            modelBuilder.Configurations.Add(new AccountCampaignJournalProgressMapping(schema));
            modelBuilder.Configurations.Add(new AccountCampaignProgressMapping(schema));
            modelBuilder.Configurations.Add(new AccountCampaignVarMapping(schema));
            modelBuilder.Configurations.Add(new AccountForumVoteMapping(schema));
            modelBuilder.Configurations.Add(new AccountIPMapping(schema));
            modelBuilder.Configurations.Add(new AccountPlanetMapping(schema));
            modelBuilder.Configurations.Add(new AccountRatingVoteMapping(schema));
            modelBuilder.Configurations.Add(new AccountRoleMapping(schema));
            modelBuilder.Configurations.Add(new AccountUnlockMapping(schema));
            modelBuilder.Configurations.Add(new AccountUserIDMapping(schema));
            modelBuilder.Configurations.Add(new AutoBanSmurfListMapping(schema));
            modelBuilder.Configurations.Add(new AutohostConfigMapping(schema));
            modelBuilder.Configurations.Add(new AvatarMapping(schema));
            modelBuilder.Configurations.Add(new BlockedCompanyMapping(schema));
            modelBuilder.Configurations.Add(new BlockedHostMapping(schema));
            modelBuilder.Configurations.Add(new CampaignMapping(schema));
            modelBuilder.Configurations.Add(new CampaignEventMapping(schema));
            modelBuilder.Configurations.Add(new CampaignJournalMapping(schema));
            modelBuilder.Configurations.Add(new CampaignJournalVarMapping(schema));
            modelBuilder.Configurations.Add(new CampaignLinkMapping(schema));
            modelBuilder.Configurations.Add(new CampaignPlanetMapping(schema));
            modelBuilder.Configurations.Add(new CampaignPlanetVarMapping(schema));
            modelBuilder.Configurations.Add(new CampaignVarMapping(schema));
            modelBuilder.Configurations.Add(new ClanMapping(schema));
            modelBuilder.Configurations.Add(new CommanderMapping(schema));
            modelBuilder.Configurations.Add(new CommanderDecorationMapping(schema));
            modelBuilder.Configurations.Add(new CommanderDecorationIconMapping(schema));
            modelBuilder.Configurations.Add(new CommanderDecorationSlotMapping(schema));
            modelBuilder.Configurations.Add(new CommanderModuleMapping(schema));
            modelBuilder.Configurations.Add(new CommanderSlotMapping(schema));
            modelBuilder.Configurations.Add(new ContributionMapping(schema));
            modelBuilder.Configurations.Add(new ContributionJarMapping(schema));
            modelBuilder.Configurations.Add(new EventMapping(schema));
            modelBuilder.Configurations.Add(new ExceptionLogMapping(schema));
            modelBuilder.Configurations.Add(new FactionMapping(schema));
            modelBuilder.Configurations.Add(new FactionTreatyMapping(schema));
            modelBuilder.Configurations.Add(new ForumCategoryMapping(schema));
            modelBuilder.Configurations.Add(new ForumLastReadMapping(schema));
            modelBuilder.Configurations.Add(new ForumPostMapping(schema));
            modelBuilder.Configurations.Add(new ForumPostEditMapping(schema));
            modelBuilder.Configurations.Add(new ForumThreadMapping(schema));
            modelBuilder.Configurations.Add(new ForumThreadLastReadMapping(schema));
            modelBuilder.Configurations.Add(new GalaxyMapping(schema));
            modelBuilder.Configurations.Add(new KudosPurchaseMapping(schema));
            modelBuilder.Configurations.Add(new LinkMapping(schema));
            modelBuilder.Configurations.Add(new LobbyChannelSubscriptionMapping(schema));
            modelBuilder.Configurations.Add(new LobbyMessageMapping(schema));
            modelBuilder.Configurations.Add(new MapRatingMapping(schema));
            modelBuilder.Configurations.Add(new MarketOfferMapping(schema));
            modelBuilder.Configurations.Add(new MiscVarMapping(schema));
            modelBuilder.Configurations.Add(new MissionMapping(schema));
            modelBuilder.Configurations.Add(new MissionScoreMapping(schema));
            modelBuilder.Configurations.Add(new NewsMapping(schema));
            modelBuilder.Configurations.Add(new PlanetMapping(schema));
            modelBuilder.Configurations.Add(new PlanetFactionMapping(schema));
            modelBuilder.Configurations.Add(new PlanetOwnerHistoryMapping(schema));
            modelBuilder.Configurations.Add(new PlanetStructureMapping(schema));
            modelBuilder.Configurations.Add(new PollMapping(schema));
            modelBuilder.Configurations.Add(new PollOptionMapping(schema));
            modelBuilder.Configurations.Add(new PollVoteMapping(schema));
            modelBuilder.Configurations.Add(new PunishmentMapping(schema));
            modelBuilder.Configurations.Add(new RatingMapping(schema));
            modelBuilder.Configurations.Add(new RatingPollMapping(schema));
            modelBuilder.Configurations.Add(new ResourceMapping(schema));
            modelBuilder.Configurations.Add(new ResourceContentFileMapping(schema));
            modelBuilder.Configurations.Add(new ResourceDependencyMapping(schema));
            modelBuilder.Configurations.Add(new ResourceSpringHashMapping(schema));
            modelBuilder.Configurations.Add(new RoleTypeMapping(schema));
            modelBuilder.Configurations.Add(new RoleTypeHierarchyMapping(schema));
            modelBuilder.Configurations.Add(new SpringBattleMapping(schema));
            modelBuilder.Configurations.Add(new SpringBattlePlayerMapping(schema));
            modelBuilder.Configurations.Add(new StructureTypeMapping(schema));
            modelBuilder.Configurations.Add(new TreatyEffectMapping(schema));
            modelBuilder.Configurations.Add(new TreatyEffectTypeMapping(schema));
            modelBuilder.Configurations.Add(new UnlockMapping(schema));
            return modelBuilder;
        }

        partial void InitializePartial();
        partial void OnModelCreatingPartial(DbModelBuilder modelBuilder);

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

        private static string ConnectionStringLocal = @"Data Source=.\SQLEXPRESS;Initial Catalog=zero-k-dev;Integrated Security=True;MultipleActiveResultSets=true";

#if !DEPLOY
        private static string ConnectionStringLive = @"Data Source=omega.licho.eu,100;Initial Catalog=zero-k;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true";
#else 
        private static string ConnectionStringLive = Settings.Default.zero_kConnectionString;
#endif


        private static bool wasDbChecked = false;
        private static object locker = new object();

#if DEBUG
        //public static bool UseLiveDb = false;
        public static bool UseLiveDb = true;
#else 
        public static bool UseLiveDb = true;
#endif



        public static Action<ZkDataContext> DataContextCreated = context => { };



        public ZkDataContext(bool? useLiveDb)
            : base(useLiveDb != null ? (useLiveDb.Value ? ConnectionStringLive : ConnectionStringLocal) : (UseLiveDb ? ConnectionStringLive : ConnectionStringLocal))
        {
#if DEBUG
            if (!wasDbChecked)
            {
                lock (locker)
                {
                    Database.CreateIfNotExists();
                    wasDbChecked = true;
                }
            }
#endif
            DataContextCreated(this);
        }


    }
}

