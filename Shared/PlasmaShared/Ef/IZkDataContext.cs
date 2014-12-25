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
    public interface IZkDataContext : IDisposable
    {
        IDbSet<AbuseReport> AbuseReports { get; set; } // AbuseReport
        IDbSet<Account> Accounts { get; set; } // Account
        IDbSet<AccountBattleAward> AccountBattleAwards { get; set; } // AccountBattleAward
        IDbSet<AccountCampaignJournalProgress> AccountCampaignJournalProgresses { get; set; } // AccountCampaignJournalProgress
        IDbSet<AccountCampaignProgress> AccountCampaignProgresses { get; set; } // AccountCampaignProgress
        IDbSet<AccountCampaignVar> AccountCampaignVars { get; set; } // AccountCampaignVar
        IDbSet<AccountForumVote> AccountForumVotes { get; set; } // AccountForumVote
        IDbSet<AccountIP> AccountIPs { get; set; } // AccountIP
        IDbSet<AccountPlanet> AccountPlanets { get; set; } // AccountPlanet
        IDbSet<AccountRatingVote> AccountRatingVotes { get; set; } // AccountRatingVote
        IDbSet<AccountRole> AccountRoles { get; set; } // AccountRole
        IDbSet<AccountUnlock> AccountUnlocks { get; set; } // AccountUnlock
        IDbSet<AccountUserID> AccountUserIDs { get; set; } // AccountUserID
        IDbSet<AutoBanSmurfList> AutoBanSmurfLists { get; set; } // AutoBanSmurfList
        IDbSet<AutohostConfig> AutohostConfigs { get; set; } // AutohostConfig
        IDbSet<Avatar> Avatars { get; set; } // Avatar
        IDbSet<BlockedCompany> BlockedCompanies { get; set; } // BlockedCompany
        IDbSet<BlockedHost> BlockedHosts { get; set; } // BlockedHost
        IDbSet<Campaign> Campaigns { get; set; } // Campaign
        IDbSet<CampaignEvent> CampaignEvents { get; set; } // CampaignEvent
        IDbSet<CampaignJournal> CampaignJournals { get; set; } // CampaignJournal
        IDbSet<CampaignJournalVar> CampaignJournalVars { get; set; } // CampaignJournalVar
        IDbSet<CampaignLink> CampaignLinks { get; set; } // CampaignLink
        IDbSet<CampaignPlanet> CampaignPlanets { get; set; } // CampaignPlanet
        IDbSet<CampaignPlanetVar> CampaignPlanetVars { get; set; } // CampaignPlanetVar
        IDbSet<CampaignVar> CampaignVars { get; set; } // CampaignVar
        IDbSet<Clan> Clans { get; set; } // Clan
        IDbSet<Commander> Commanders { get; set; } // Commander
        IDbSet<CommanderDecoration> CommanderDecorations { get; set; } // CommanderDecoration
        IDbSet<CommanderDecorationIcon> CommanderDecorationIcons { get; set; } // CommanderDecorationIcon
        IDbSet<CommanderDecorationSlot> CommanderDecorationSlots { get; set; } // CommanderDecorationSlot
        IDbSet<CommanderModule> CommanderModules { get; set; } // CommanderModule
        IDbSet<CommanderSlot> CommanderSlots { get; set; } // CommanderSlot
        IDbSet<Contribution> Contributions { get; set; } // Contribution
        IDbSet<ContributionJar> ContributionJars { get; set; } // ContributionJar
        IDbSet<Event> Events { get; set; } // Event
        IDbSet<ExceptionLog> ExceptionLogs { get; set; } // ExceptionLog
        IDbSet<Faction> Factions { get; set; } // Faction
        IDbSet<FactionTreaty> FactionTreaties { get; set; } // FactionTreaty
        IDbSet<ForumCategory> ForumCategories { get; set; } // ForumCategory
        IDbSet<ForumLastRead> ForumLastReads { get; set; } // ForumLastRead
        IDbSet<ForumPost> ForumPosts { get; set; } // ForumPost
        IDbSet<ForumPostEdit> ForumPostEdits { get; set; } // ForumPostEdit
        IDbSet<ForumThread> ForumThreads { get; set; } // ForumThread
        IDbSet<ForumThreadLastRead> ForumThreadLastReads { get; set; } // ForumThreadLastRead
        IDbSet<Galaxy> Galaxies { get; set; } // Galaxy
        IDbSet<KudosPurchase> KudosPurchases { get; set; } // KudosPurchase
        IDbSet<Link> Links { get; set; } // Link
        IDbSet<LobbyChannelSubscription> LobbyChannelSubscriptions { get; set; } // LobbyChannelSubscription
        IDbSet<LobbyMessage> LobbyMessages { get; set; } // LobbyMessage
        IDbSet<MapRating> MapRatings { get; set; } // MapRating
        IDbSet<MarketOffer> MarketOffers { get; set; } // MarketOffer
        IDbSet<MiscVar> MiscVars { get; set; } // MiscVar
        IDbSet<Mission> Missions { get; set; } // Mission
        IDbSet<MissionScore> MissionScores { get; set; } // MissionScore
        IDbSet<News> News { get; set; } // News
        IDbSet<Planet> Planets { get; set; } // Planet
        IDbSet<PlanetFaction> PlanetFactions { get; set; } // PlanetFaction
        IDbSet<PlanetOwnerHistory> PlanetOwnerHistories { get; set; } // PlanetOwnerHistory
        IDbSet<PlanetStructure> PlanetStructures { get; set; } // PlanetStructure
        IDbSet<Poll> Polls { get; set; } // Poll
        IDbSet<PollOption> PollOptions { get; set; } // PollOption
        IDbSet<PollVote> PollVotes { get; set; } // PollVote
        IDbSet<Punishment> Punishments { get; set; } // Punishment
        IDbSet<Rating> Ratings { get; set; } // Rating
        IDbSet<RatingPoll> RatingPolls { get; set; } // RatingPoll
        IDbSet<Resource> Resources { get; set; } // Resource
        IDbSet<ResourceContentFile> ResourceContentFiles { get; set; } // ResourceContentFile
        IDbSet<ResourceDependency> ResourceDependencies { get; set; } // ResourceDependency
        IDbSet<ResourceSpringHash> ResourceSpringHashes { get; set; } // ResourceSpringHash
        IDbSet<RoleType> RoleTypes { get; set; } // RoleType
        IDbSet<RoleTypeHierarchy> RoleTypeHierarchies { get; set; } // RoleTypeHierarchy
        IDbSet<SpringBattle> SpringBattles { get; set; } // SpringBattle
        IDbSet<SpringBattlePlayer> SpringBattlePlayers { get; set; } // SpringBattlePlayer
        IDbSet<StructureType> StructureTypes { get; set; } // StructureType
        IDbSet<sysdiagram> sysdiagrams { get; set; } // sysdiagrams
        IDbSet<TreatyEffect> TreatyEffects { get; set; } // TreatyEffect
        IDbSet<TreatyEffectType> TreatyEffectTypes { get; set; } // TreatyEffectType
        IDbSet<Unlock> Unlocks { get; set; } // Unlock

        int SaveChanges();
    }

}
