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
    // Account
    public partial class Account
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public string Name { get; set; } // Name
        public string Email { get; set; } // Email
        public DateTime FirstLogin { get; set; } // FirstLogin
        public DateTime LastLogin { get; set; } // LastLogin
        public string Aliases { get; set; } // Aliases
        public double Elo { get; set; } // Elo
        public double EloWeight { get; set; } // EloWeight
        public double Elo1v1 { get; set; } // Elo1v1
        public double Elo1v1Weight { get; set; } // Elo1v1Weight
        public double EloPw { get; set; } // EloPw
        public bool IsLobbyAdministrator { get; set; } // IsLobbyAdministrator
        public bool IsBot { get; set; } // IsBot
        public string Password { get; set; } // Password
        public string Country { get; set; } // Country
        public int LobbyTimeRank { get; set; } // LobbyTimeRank
        public int MissionRunCount { get; set; } // MissionRunCount
        public bool IsZeroKAdmin { get; set; } // IsZeroKAdmin
        public int XP { get; set; } // XP
        public int Level { get; set; } // Level
        public int? ClanID { get; set; } // ClanID
        public DateTime? LastNewsRead { get; set; } // LastNewsRead
        public int? FactionID { get; set; } // FactionID
        public int? LobbyID { get; set; } // LobbyID
        public bool IsDeleted { get; set; } // IsDeleted
        public string Avatar { get; set; } // Avatar
        public int SpringieLevel { get; set; } // SpringieLevel
        public string LobbyVersion { get; set; } // LobbyVersion
        public double PwDropshipsProduced { get; set; } // PwDropshipsProduced
        public double PwDropshipsUsed { get; set; } // PwDropshipsUsed
        public double PwBombersProduced { get; set; } // PwBombersProduced
        public double PwBombersUsed { get; set; } // PwBombersUsed
        public double PwMetalProduced { get; set; } // PwMetalProduced
        public double PwMetalUsed { get; set; } // PwMetalUsed
        public double PwWarpProduced { get; set; } // PwWarpProduced
        public double PwWarpUsed { get; set; } // PwWarpUsed
        public double PwAttackPoints { get; set; } // PwAttackPoints
        public DateTime? LastLobbyVersionCheck { get; set; } // LastLobbyVersionCheck
        public string Language { get; set; } // Language
        public bool HasVpnException { get; set; } // HasVpnException
        public int Kudos { get; set; } // Kudos
        public int ForumTotalUpvotes { get; set; } // ForumTotalUpvotes
        public int ForumTotalDownvotes { get; set; } // ForumTotalDownvotes
        public int? VotesAvailable { get; set; } // VotesAvailable
        public decimal? SteamID { get; set; } // SteamID
        public string SteamName { get; set; } // SteamName

        // Reverse navigation
        public virtual AutoBanSmurfList AutoBanSmurfList { get; set; } // AutoBanSmurfList.FK_AutoBanSmurfList_Account
        public virtual ICollection<AbuseReport> AbuseReportsByAccountID { get; set; } // AbuseReport.FK_AbuseReport_Account
        public virtual ICollection<AbuseReport> AbuseReports_ReporterAccountID { get; set; } // AbuseReport.FK_AbuseReport_Account1
        public virtual ICollection<AccountBattleAward> AccountBattleAwards { get; set; } // Many to many mapping
        public virtual ICollection<AccountCampaignJournalProgress> AccountCampaignJournalProgresses { get; set; } // Many to many mapping
        public virtual ICollection<AccountCampaignProgress> AccountCampaignProgress { get; set; } // Many to many mapping
        public virtual ICollection<AccountCampaignVar> AccountCampaignVars { get; set; } // Many to many mapping
        public virtual ICollection<AccountForumVote> AccountForumVotes { get; set; } // Many to many mapping
        public virtual ICollection<AccountIP> AccountIPS { get; set; } // Many to many mapping
        public virtual ICollection<AccountPlanet> AccountPlanets { get; set; } // Many to many mapping
        public virtual ICollection<AccountRatingVote> AccountRatingVotes { get; set; } // Many to many mapping
        public virtual ICollection<AccountRole> AccountRolesByAccountID { get; set; } // Many to many mapping
        public virtual ICollection<AccountUnlock> AccountUnlocks { get; set; } // Many to many mapping
        public virtual ICollection<AccountUserID> AccountUserIDS { get; set; } // Many to many mapping
        public virtual ICollection<CampaignEvent> CampaignEvents { get; set; } // CampaignEvent.FK_CampaignEvent_Account
        public virtual ICollection<Commander> Commanders { get; set; } // Commander.FK_Commander_Account
        public virtual ICollection<Contribution> ContributionsByAccountID { get; set; } // Contribution.FK_Contribution_Account
        public virtual ICollection<Contribution> Contributions_ManuallyAddedAccountID { get; set; } // Contribution.FK_Contribution_Account1
        public virtual ICollection<ContributionJar> ContributionJars { get; set; } // ContributionJar.FK_ContributionJar_Account
        public virtual ICollection<Event> Events { get; set; } // Many to many mapping
        public virtual ICollection<FactionTreaty> FactionTreaties_AcceptedAccountID { get; set; } // FactionTreaty.FK_FactionTreaty_Account1
        public virtual ICollection<FactionTreaty> FactionTreaties_ProposingAccountID { get; set; } // FactionTreaty.FK_FactionTreaty_Account
        public virtual ICollection<ForumLastRead> ForumLastReads { get; set; } // Many to many mapping
        public virtual ICollection<ForumPostEdit> ForumPostEdits { get; set; } // ForumPostEdit.FK_ForumPostEdit_Account
        public virtual ICollection<ForumThread> ForumThreads_CreatedAccountID { get; set; } // ForumThread.FK_ForumThread_Account
        public virtual ICollection<ForumThread> ForumThreadsByLastPostAccountID { get; set; } // ForumThread.FK_ForumThread_Account1
        public virtual ICollection<ForumThreadLastRead> ForumThreadLastReads { get; set; } // Many to many mapping
        public virtual ICollection<KudosPurchase> KudosPurchases { get; set; } // KudosPurchase.FK_KudosChange_Account
        public virtual ICollection<LobbyChannelSubscription> LobbyChannelSubscriptions { get; set; } // Many to many mapping
        public virtual ICollection<MapRating> MapRatings { get; set; } // Many to many mapping
        public virtual ICollection<MarketOffer> MarketOffers_AcceptedAccountID { get; set; } // MarketOffer.FK_MarketOffer_Player1
        public virtual ICollection<MarketOffer> MarketOffers_AccountID { get; set; } // MarketOffer.FK_MarketOffer_Player
        public virtual ICollection<Mission> Missions { get; set; } // Mission.FK_Mission_Account
        public virtual ICollection<MissionScore> MissionScores { get; set; } // Many to many mapping
        public virtual ICollection<News> News { get; set; } // News.FK_News_Account
        public virtual ICollection<Planet> Planets { get; set; } // Planet.FK_Planet_Account
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; } // PlanetOwnerHistory.FK_PlanetOwnerHistory_Account
        public virtual ICollection<PlanetStructure> PlanetStructures { get; set; } // PlanetStructure.FK_PlanetStructure_Account
        public virtual ICollection<Poll> Polls_CreatedAccountID { get; set; } // Poll.FK_Poll_Account1
        public virtual ICollection<Poll> PollsByRoleTargetAccountID { get; set; } // Poll.FK_Poll_Account
        public virtual ICollection<PollVote> PollVotes { get; set; } // Many to many mapping
        public virtual ICollection<Punishment> PunishmentsByAccountID { get; set; } // Punishment.FK_Punishment_Account
        public virtual ICollection<Punishment> Punishments_CreatedAccountID { get; set; } // Punishment.FK_Punishment_Account1
        public virtual ICollection<Rating> Ratings { get; set; } // Rating.FK_Rating_Account
        public virtual ICollection<Resource> Resources { get; set; } // Resource.FK_Resource_Account
        public virtual ICollection<SpringBattle> SpringBattles { get; set; } // SpringBattle.FK_SpringBattle_Account
        public virtual ICollection<SpringBattlePlayer> SpringBattlePlayers { get; set; } // Many to many mapping
        public virtual ICollection<ForumPost> ForumPosts { get; set; }

        // Foreign keys
        public virtual Faction Faction { get; set; } // FK_Account_Faction
        [ForeignKey("ClanID")]
        public virtual Clan Clan { get; set; }


        public Account()
        {
            Elo = 1500;
            EloWeight = 1;
            Elo1v1 = 1500;
            Elo1v1Weight = 1;
            EloPw = 1500;
            IsLobbyAdministrator = false;
            IsBot = false;
            LobbyTimeRank = 0;
            MissionRunCount = 0;
            IsZeroKAdmin = false;
            XP = 0;
            Level = 0;
            IsDeleted = false;
            SpringieLevel = 1;
            PwDropshipsProduced = 0;
            PwDropshipsUsed = 0;
            PwBombersProduced = 0;
            PwBombersUsed = 0;
            PwMetalProduced = 0;
            PwMetalUsed = 0;
            PwWarpProduced = 0;
            PwWarpUsed = 0;
            PwAttackPoints = 0;
            HasVpnException = false;
            Kudos = 0;
            ForumTotalUpvotes = 0;
            ForumTotalDownvotes = 0;
            FirstLogin = DateTime.UtcNow;

            AbuseReportsByAccountID = new List<AbuseReport>();
            AbuseReports_ReporterAccountID = new List<AbuseReport>();
            AccountBattleAwards = new List<AccountBattleAward>();
            AccountCampaignJournalProgresses = new List<AccountCampaignJournalProgress>();
            AccountCampaignProgress = new List<AccountCampaignProgress>();
            AccountCampaignVars = new List<AccountCampaignVar>();
            AccountForumVotes = new List<AccountForumVote>();
            AccountIPS = new List<AccountIP>();
            AccountPlanets = new List<AccountPlanet>();
            AccountRatingVotes = new List<AccountRatingVote>();
            AccountRolesByAccountID = new List<AccountRole>();
            AccountUnlocks = new List<AccountUnlock>();
            AccountUserIDS = new List<AccountUserID>();
            CampaignEvents = new List<CampaignEvent>();
            Commanders = new List<Commander>();
            ContributionsByAccountID = new List<Contribution>();
            Contributions_ManuallyAddedAccountID = new List<Contribution>();
            ContributionJars = new List<ContributionJar>();
            FactionTreaties_AcceptedAccountID = new List<FactionTreaty>();
            FactionTreaties_ProposingAccountID = new List<FactionTreaty>();
            ForumLastReads = new List<ForumLastRead>();
            ForumPostEdits = new List<ForumPostEdit>();
            ForumThreads_CreatedAccountID = new List<ForumThread>();
            ForumThreadsByLastPostAccountID = new List<ForumThread>();
            ForumThreadLastReads = new List<ForumThreadLastRead>();
            KudosPurchases = new List<KudosPurchase>();
            LobbyChannelSubscriptions = new List<LobbyChannelSubscription>();
            MapRatings = new List<MapRating>();
            MarketOffers_AcceptedAccountID = new List<MarketOffer>();
            MarketOffers_AccountID = new List<MarketOffer>();
            Missions = new List<Mission>();
            MissionScores = new List<MissionScore>();
            News = new List<News>();
            Planets = new List<Planet>();
            PlanetOwnerHistories = new List<PlanetOwnerHistory>();
            PlanetStructures = new List<PlanetStructure>();
            Polls_CreatedAccountID = new List<Poll>();
            PollsByRoleTargetAccountID = new List<Poll>();
            PollVotes = new List<PollVote>();
            PunishmentsByAccountID = new List<Punishment>();
            Punishments_CreatedAccountID = new List<Punishment>();
            Ratings = new List<Rating>();
            Resources = new List<Resource>();
            SpringBattles = new List<SpringBattle>();
            SpringBattlePlayers = new List<SpringBattlePlayer>();
            Events = new List<Event>();
            ForumPosts = new List<ForumPost>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
