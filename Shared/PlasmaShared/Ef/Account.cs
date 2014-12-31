using System.Text;

namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Account")]
    public partial class Account
    {

        public Account()
        {
            AbuseReportsByAccountID = new HashSet<AbuseReport>();
            AbuseReportsByReporterAccountID = new HashSet<AbuseReport>();
            AccountBattleAwards = new HashSet<AccountBattleAward>();
            AccountCampaignJournalProgresses = new HashSet<AccountCampaignJournalProgress>();
            AccountCampaignProgress = new HashSet<AccountCampaignProgress>();
            AccountCampaignVars = new HashSet<AccountCampaignVar>();
            AccountForumVotes = new HashSet<AccountForumVote>();
            AccountIPs = new HashSet<AccountIP>();
            AccountRatingVotes = new HashSet<AccountRatingVote>();
            AccountRolesByAccountID = new HashSet<AccountRole>();
            AccountUnlocks = new HashSet<AccountUnlock>();
            AccountUserIDs = new HashSet<AccountUserID>();
            CampaignEvents = new HashSet<CampaignEvent>();
            Commanders = new HashSet<Commander>();
            ContributionsByAccountID = new HashSet<Contribution>();
            Contributions1 = new HashSet<Contribution>();
            ContributionJars = new HashSet<ContributionJar>();
            FactionTreatiesByProposingAccount = new HashSet<FactionTreaty>();
            FactionTreatiesByAcceptedAccountID = new HashSet<FactionTreaty>();
            ForumLastReads = new HashSet<ForumLastRead>();
            ForumPosts = new HashSet<ForumPost>();
            ForumPostEdits = new HashSet<ForumPostEdit>();
            ForumThreads = new HashSet<ForumThread>();
            ForumThreadsByLastPostAccountID = new HashSet<ForumThread>();
            ForumThreadLastReads = new HashSet<ForumThreadLastRead>();
            KudosPurchases = new HashSet<KudosPurchase>();
            LobbyChannelSubscriptions = new HashSet<LobbyChannelSubscription>();
            MarketOffers = new HashSet<MarketOffer>();
            MarketOffers1 = new HashSet<MarketOffer>();
            Missions = new HashSet<Mission>();
            MissionScores = new HashSet<MissionScore>();
            News = new HashSet<News>();
            Planets = new HashSet<Planet>();
            PlanetOwnerHistories = new HashSet<PlanetOwnerHistory>();
            PlanetStructures = new HashSet<PlanetStructure>();
            AccountPlanets = new HashSet<AccountPlanet>();
            PollsByRoleTargetAccountID = new HashSet<Poll>();
            PollsByCreatedAccountID = new HashSet<Poll>();
            PollVotes = new HashSet<PollVote>();
            PunishmentsByAccountID = new HashSet<Punishment>();
            PunishmentsByCreatedAccountID = new HashSet<Punishment>();
            Ratings = new HashSet<Rating>();
            Resources = new HashSet<Resource>();
            MapRatings = new HashSet<MapRating>();
            SpringBattles = new HashSet<SpringBattle>();
            SpringBattlePlayers = new HashSet<SpringBattlePlayer>();
            Events = new HashSet<Event>();
        }

        public int AccountID { get; set; }

        [Required]
        [StringLength(200)]
        [Index]
        public string Name { get; set; }

        public string Email { get; set; }

        public DateTime FirstLogin { get; set; }

        public DateTime LastLogin { get; set; }

        public string Aliases { get; set; }

        public double Elo { get; set; }

        public double EloWeight { get; set; }

        public double Elo1v1 { get; set; }

        public double Elo1v1Weight { get; set; }

        public double EloPw { get; set; }

        public bool IsLobbyAdministrator { get; set; }

        public bool IsBot { get; set; }

        [StringLength(100)]
        public string Password { get; set; }

        [StringLength(5)]
        public string Country { get; set; }

        public int LobbyTimeRank { get; set; }

        public int MissionRunCount { get; set; }

        public bool IsZeroKAdmin { get; set; }

        public int Xp { get; set; }

        public int Level { get; set; }

        public int? ClanID { get; set; }

        public DateTime? LastNewsRead { get; set; }

        public int? FactionID { get; set; }

        [Index]
        public int? LobbyID { get; set; }

        public bool IsDeleted { get; set; }

        [StringLength(50)]
        public string Avatar { get; set; }

        public int SpringieLevel { get; set; }

        [StringLength(200)]
        public string LobbyVersion { get; set; }

        public double PwDropshipsProduced { get; set; }

        public double PwDropshipsUsed { get; set; }

        public double PwBombersProduced { get; set; }

        public double PwBombersUsed { get; set; }

        public double PwMetalProduced { get; set; }

        public double PwMetalUsed { get; set; }

        public double PwWarpProduced { get; set; }

        public double PwWarpUsed { get; set; }

        public double PwAttackPoints { get; set; }

        public DateTime? LastLobbyVersionCheck { get; set; }

        [StringLength(2)]
        public string Language { get; set; }

        public bool HasVpnException { get; set; }

        public int Kudos { get; set; }

        public int ForumTotalUpvotes { get; set; }

        public int ForumTotalDownvotes { get; set; }

        public int? VotesAvailable { get; set; }

        public decimal? SteamID { get; set; }

        [StringLength(200)]
        public string SteamName { get; set; }

        public virtual ICollection<AbuseReport> AbuseReportsByAccountID { get; set; }

        public virtual ICollection<AbuseReport> AbuseReportsByReporterAccountID { get; set; }

        public virtual Faction Faction { get; set; }

        public virtual ICollection<AccountBattleAward> AccountBattleAwards { get; set; }

        public virtual ICollection<AccountCampaignJournalProgress> AccountCampaignJournalProgresses { get; set; }

        public virtual ICollection<AccountCampaignProgress> AccountCampaignProgress { get; set; }

        public virtual ICollection<AccountCampaignVar> AccountCampaignVars { get; set; }

        public virtual ICollection<AccountForumVote> AccountForumVotes { get; set; }

        public virtual ICollection<AccountIP> AccountIPs { get; set; }

        public virtual ICollection<AccountRatingVote> AccountRatingVotes { get; set; }

        public virtual ICollection<AccountRole> AccountRolesByAccountID { get; set; }

        public virtual ICollection<AccountUnlock> AccountUnlocks { get; set; }

        public virtual ICollection<AccountUserID> AccountUserIDs { get; set; }

        public virtual AutoBanSmurfList AutoBanSmurfList { get; set; }

        public virtual ICollection<CampaignEvent> CampaignEvents { get; set; }

        public virtual ICollection<Commander> Commanders { get; set; }

        public virtual ICollection<Contribution> ContributionsByAccountID { get; set; }

        public virtual ICollection<Contribution> Contributions1 { get; set; }

        public virtual ICollection<ContributionJar> ContributionJars { get; set; }

        public virtual ICollection<FactionTreaty> FactionTreatiesByProposingAccount { get; set; }

        public virtual ICollection<FactionTreaty> FactionTreatiesByAcceptedAccountID { get; set; }

        public virtual ICollection<ForumLastRead> ForumLastReads { get; set; }

        public virtual ICollection<ForumPostEdit> ForumPostEdits { get; set; }

        public virtual ICollection<ForumPost> ForumPosts { get; set; }

        public virtual ICollection<ForumThread> ForumThreads { get; set; }

        public virtual ICollection<ForumThread> ForumThreadsByLastPostAccountID { get; set; }

        public virtual ICollection<ForumThreadLastRead> ForumThreadLastReads { get; set; }

        public virtual ICollection<KudosPurchase> KudosPurchases { get; set; }

        public virtual ICollection<LobbyChannelSubscription> LobbyChannelSubscriptions { get; set; }

        public virtual ICollection<MarketOffer> MarketOffers { get; set; }

        public virtual ICollection<MarketOffer> MarketOffers1 { get; set; }

        public virtual ICollection<Mission> Missions { get; set; }

        public virtual ICollection<MissionScore> MissionScores { get; set; }

        public virtual ICollection<News> News { get; set; }

        public virtual ICollection<Planet> Planets { get; set; }

        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }

        public virtual ICollection<PlanetStructure> PlanetStructures { get; set; }

        public virtual ICollection<AccountPlanet> AccountPlanets { get; set; }

        public virtual ICollection<Poll> PollsByRoleTargetAccountID { get; set; }

        public virtual ICollection<Poll> PollsByCreatedAccountID { get; set; }

        public virtual ICollection<PollVote> PollVotes { get; set; }

        public virtual ICollection<Punishment> PunishmentsByAccountID { get; set; }

        public virtual ICollection<Punishment> PunishmentsByCreatedAccountID { get; set; }

        public virtual ICollection<Rating> Ratings { get; set; }

        public virtual ICollection<Resource> Resources { get; set; }

        public virtual ICollection<MapRating> MapRatings { get; set; }

        public virtual ICollection<SpringBattle> SpringBattles { get; set; }

        public virtual ICollection<SpringBattlePlayer> SpringBattlePlayers { get; set; }

        public virtual ICollection<Event> Events { get; set; }

        public virtual Clan Clan { get; set; }
    }
}
