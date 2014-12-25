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
    public class FakeZkDataContext : IZkDataContext
    {
        public IDbSet<AbuseReport> AbuseReports { get; set; }
        public IDbSet<Account> Accounts { get; set; }
        public IDbSet<AccountBattleAward> AccountBattleAwards { get; set; }
        public IDbSet<AccountCampaignJournalProgress> AccountCampaignJournalProgress { get; set; }
        public IDbSet<AccountCampaignProgress> AccountCampaignProgress { get; set; }
        public IDbSet<AccountCampaignVar> AccountCampaignVars { get; set; }
        public IDbSet<AccountForumVote> AccountForumVotes { get; set; }
        public IDbSet<AccountIP> AccountIPS { get; set; }
        public IDbSet<AccountPlanet> AccountPlanets { get; set; }
        public IDbSet<AccountRatingVote> AccountRatingVotes { get; set; }
        public IDbSet<AccountRole> AccountRoles { get; set; }
        public IDbSet<AccountUnlock> AccountUnlocks { get; set; }
        public IDbSet<AccountUserID> AccountUserIDS { get; set; }
        public IDbSet<AutoBanSmurfList> AutoBanSmurfLists { get; set; }
        public IDbSet<AutohostConfig> AutohostConfigs { get; set; }
        public IDbSet<Avatar> Avatars { get; set; }
        public IDbSet<BlockedCompany> BlockedCompanies { get; set; }
        public IDbSet<BlockedHost> BlockedHosts { get; set; }
        public IDbSet<Campaign> Campaigns { get; set; }
        public IDbSet<CampaignEvent> CampaignEvents { get; set; }
        public IDbSet<CampaignJournal> CampaignJournals { get; set; }
        public IDbSet<CampaignJournalVar> CampaignJournalVars { get; set; }
        public IDbSet<CampaignLink> CampaignLinks { get; set; }
        public IDbSet<CampaignPlanet> CampaignPlanets { get; set; }
        public IDbSet<CampaignPlanetVar> CampaignPlanetVars { get; set; }
        public IDbSet<CampaignVar> CampaignVars { get; set; }
        public IDbSet<Clan> Clans { get; set; }
        public IDbSet<Commander> Commanders { get; set; }
        public IDbSet<CommanderDecoration> CommanderDecorations { get; set; }
        public IDbSet<CommanderDecorationIcon> CommanderDecorationIcons { get; set; }
        public IDbSet<CommanderDecorationSlot> CommanderDecorationSlots { get; set; }
        public IDbSet<CommanderModule> CommanderModules { get; set; }
        public IDbSet<CommanderSlot> CommanderSlots { get; set; }
        public IDbSet<Contribution> Contributions { get; set; }
        public IDbSet<ContributionJar> ContributionJars { get; set; }
        public IDbSet<Event> Events { get; set; }
        public IDbSet<ExceptionLog> ExceptionLogs { get; set; }
        public IDbSet<Faction> Factions { get; set; }
        public IDbSet<FactionTreaty> FactionTreaties { get; set; }
        public IDbSet<ForumCategory> ForumCategories { get; set; }
        public IDbSet<ForumLastRead> ForumLastReads { get; set; }
        public IDbSet<ForumPost> ForumPosts { get; set; }
        public IDbSet<ForumPostEdit> ForumPostEdits { get; set; }
        public IDbSet<ForumThread> ForumThreads { get; set; }
        public IDbSet<ForumThreadLastRead> ForumThreadLastReads { get; set; }
        public IDbSet<Galaxy> Galaxies { get; set; }
        public IDbSet<KudosPurchase> KudosPurchases { get; set; }
        public IDbSet<Link> Links { get; set; }
        public IDbSet<LobbyChannelSubscription> LobbyChannelSubscriptions { get; set; }
        public IDbSet<LobbyMessage> LobbyMessages { get; set; }
        public IDbSet<MapRating> MapRatings { get; set; }
        public IDbSet<MarketOffer> MarketOffers { get; set; }
        public IDbSet<MiscVar> MiscVars { get; set; }
        public IDbSet<Mission> Missions { get; set; }
        public IDbSet<MissionScore> MissionScores { get; set; }
        public IDbSet<News> News { get; set; }
        public IDbSet<Planet> Planets { get; set; }
        public IDbSet<PlanetFaction> PlanetFactions { get; set; }
        public IDbSet<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }
        public IDbSet<PlanetStructure> PlanetStructures { get; set; }
        public IDbSet<Poll> Polls { get; set; }
        public IDbSet<PollOption> PollOptions { get; set; }
        public IDbSet<PollVote> PollVotes { get; set; }
        public IDbSet<Punishment> Punishments { get; set; }
        public IDbSet<Rating> Ratings { get; set; }
        public IDbSet<RatingPoll> RatingPolls { get; set; }
        public IDbSet<Resource> Resources { get; set; }
        public IDbSet<ResourceContentFile> ResourceContentFiles { get; set; }
        public IDbSet<ResourceDependency> ResourceDependencies { get; set; }
        public IDbSet<ResourceSpringHash> ResourceSpringHashes { get; set; }
        public IDbSet<RoleType> RoleTypes { get; set; }
        public IDbSet<RoleTypeHierarchy> RoleTypeHierarchies { get; set; }
        public IDbSet<SpringBattle> SpringBattles { get; set; }
        public IDbSet<SpringBattlePlayer> SpringBattlePlayers { get; set; }
        public IDbSet<StructureType> StructureTypes { get; set; }
        public IDbSet<sysdiagram> sysdiagrams { get; set; }
        public IDbSet<TreatyEffect> TreatyEffects { get; set; }
        public IDbSet<TreatyEffectType> TreatyEffectTypes { get; set; }
        public IDbSet<Unlock> Unlocks { get; set; }

        public FakeZkDataContext()
        {
            AbuseReports = new FakeDbSet<AbuseReport>();
            Accounts = new FakeDbSet<Account>();
            AccountBattleAwards = new FakeDbSet<AccountBattleAward>();
            AccountCampaignJournalProgress = new FakeDbSet<AccountCampaignJournalProgress>();
            AccountCampaignProgress = new FakeDbSet<AccountCampaignProgress>();
            AccountCampaignVars = new FakeDbSet<AccountCampaignVar>();
            AccountForumVotes = new FakeDbSet<AccountForumVote>();
            AccountIPS = new FakeDbSet<AccountIP>();
            AccountPlanets = new FakeDbSet<AccountPlanet>();
            AccountRatingVotes = new FakeDbSet<AccountRatingVote>();
            AccountRoles = new FakeDbSet<AccountRole>();
            AccountUnlocks = new FakeDbSet<AccountUnlock>();
            AccountUserIDS = new FakeDbSet<AccountUserID>();
            AutoBanSmurfLists = new FakeDbSet<AutoBanSmurfList>();
            AutohostConfigs = new FakeDbSet<AutohostConfig>();
            Avatars = new FakeDbSet<Avatar>();
            BlockedCompanies = new FakeDbSet<BlockedCompany>();
            BlockedHosts = new FakeDbSet<BlockedHost>();
            Campaigns = new FakeDbSet<Campaign>();
            CampaignEvents = new FakeDbSet<CampaignEvent>();
            CampaignJournals = new FakeDbSet<CampaignJournal>();
            CampaignJournalVars = new FakeDbSet<CampaignJournalVar>();
            CampaignLinks = new FakeDbSet<CampaignLink>();
            CampaignPlanets = new FakeDbSet<CampaignPlanet>();
            CampaignPlanetVars = new FakeDbSet<CampaignPlanetVar>();
            CampaignVars = new FakeDbSet<CampaignVar>();
            Clans = new FakeDbSet<Clan>();
            Commanders = new FakeDbSet<Commander>();
            CommanderDecorations = new FakeDbSet<CommanderDecoration>();
            CommanderDecorationIcons = new FakeDbSet<CommanderDecorationIcon>();
            CommanderDecorationSlots = new FakeDbSet<CommanderDecorationSlot>();
            CommanderModules = new FakeDbSet<CommanderModule>();
            CommanderSlots = new FakeDbSet<CommanderSlot>();
            Contributions = new FakeDbSet<Contribution>();
            ContributionJars = new FakeDbSet<ContributionJar>();
            Events = new FakeDbSet<Event>();
            ExceptionLogs = new FakeDbSet<ExceptionLog>();
            Factions = new FakeDbSet<Faction>();
            FactionTreaties = new FakeDbSet<FactionTreaty>();
            ForumCategories = new FakeDbSet<ForumCategory>();
            ForumLastReads = new FakeDbSet<ForumLastRead>();
            ForumPosts = new FakeDbSet<ForumPost>();
            ForumPostEdits = new FakeDbSet<ForumPostEdit>();
            ForumThreads = new FakeDbSet<ForumThread>();
            ForumThreadLastReads = new FakeDbSet<ForumThreadLastRead>();
            Galaxies = new FakeDbSet<Galaxy>();
            KudosPurchases = new FakeDbSet<KudosPurchase>();
            Links = new FakeDbSet<Link>();
            LobbyChannelSubscriptions = new FakeDbSet<LobbyChannelSubscription>();
            LobbyMessages = new FakeDbSet<LobbyMessage>();
            MapRatings = new FakeDbSet<MapRating>();
            MarketOffers = new FakeDbSet<MarketOffer>();
            MiscVars = new FakeDbSet<MiscVar>();
            Missions = new FakeDbSet<Mission>();
            MissionScores = new FakeDbSet<MissionScore>();
            News = new FakeDbSet<News>();
            Planets = new FakeDbSet<Planet>();
            PlanetFactions = new FakeDbSet<PlanetFaction>();
            PlanetOwnerHistories = new FakeDbSet<PlanetOwnerHistory>();
            PlanetStructures = new FakeDbSet<PlanetStructure>();
            Polls = new FakeDbSet<Poll>();
            PollOptions = new FakeDbSet<PollOption>();
            PollVotes = new FakeDbSet<PollVote>();
            Punishments = new FakeDbSet<Punishment>();
            Ratings = new FakeDbSet<Rating>();
            RatingPolls = new FakeDbSet<RatingPoll>();
            Resources = new FakeDbSet<Resource>();
            ResourceContentFiles = new FakeDbSet<ResourceContentFile>();
            ResourceDependencies = new FakeDbSet<ResourceDependency>();
            ResourceSpringHashes = new FakeDbSet<ResourceSpringHash>();
            RoleTypes = new FakeDbSet<RoleType>();
            RoleTypeHierarchies = new FakeDbSet<RoleTypeHierarchy>();
            SpringBattles = new FakeDbSet<SpringBattle>();
            SpringBattlePlayers = new FakeDbSet<SpringBattlePlayer>();
            StructureTypes = new FakeDbSet<StructureType>();
            sysdiagrams = new FakeDbSet<sysdiagram>();
            TreatyEffects = new FakeDbSet<TreatyEffect>();
            TreatyEffectTypes = new FakeDbSet<TreatyEffectType>();
            Unlocks = new FakeDbSet<Unlock>();
        }

        public int SaveChanges()
        {
            return 0;
        }

        public void Dispose()
        {
            throw new NotImplementedException(); 
        }
    }
}
