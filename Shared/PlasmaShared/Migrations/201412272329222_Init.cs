namespace PlasmaShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AbuseReport",
                c => new
                    {
                        AbuseReportID = c.Int(nullable: false, identity: true),
                        AccountID = c.Int(nullable: false),
                        ReporterAccountID = c.Int(nullable: false),
                        Time = c.DateTime(nullable: false),
                        Text = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.AbuseReportID)
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Account", t => t.ReporterAccountID)
                .Index(t => t.AccountID)
                .Index(t => t.ReporterAccountID);
            
            CreateTable(
                "dbo.Account",
                c => new
                    {
                        AccountID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 200, unicode: false),
                        Email = c.String(),
                        FirstLogin = c.DateTime(nullable: false),
                        LastLogin = c.DateTime(nullable: false),
                        Aliases = c.String(),
                        Elo = c.Double(nullable: false),
                        EloWeight = c.Double(nullable: false),
                        Elo1v1 = c.Double(nullable: false),
                        Elo1v1Weight = c.Double(nullable: false),
                        EloPw = c.Double(nullable: false),
                        IsLobbyAdministrator = c.Boolean(nullable: false),
                        IsBot = c.Boolean(nullable: false),
                        Password = c.String(maxLength: 100, unicode: false),
                        Country = c.String(maxLength: 5, unicode: false),
                        LobbyTimeRank = c.Int(nullable: false),
                        MissionRunCount = c.Int(nullable: false),
                        IsZeroKAdmin = c.Boolean(nullable: false),
                        XP = c.Int(nullable: false),
                        Level = c.Int(nullable: false),
                        ClanID = c.Int(),
                        LastNewsRead = c.DateTime(),
                        FactionID = c.Int(),
                        LobbyID = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        Avatar = c.String(maxLength: 50),
                        SpringieLevel = c.Int(nullable: false),
                        LobbyVersion = c.String(maxLength: 200),
                        PwDropshipsProduced = c.Double(nullable: false),
                        PwDropshipsUsed = c.Double(nullable: false),
                        PwBombersProduced = c.Double(nullable: false),
                        PwBombersUsed = c.Double(nullable: false),
                        PwMetalProduced = c.Double(nullable: false),
                        PwMetalUsed = c.Double(nullable: false),
                        PwWarpProduced = c.Double(nullable: false),
                        PwWarpUsed = c.Double(nullable: false),
                        PwAttackPoints = c.Double(nullable: false),
                        LastLobbyVersionCheck = c.DateTime(),
                        Language = c.String(maxLength: 2),
                        HasVpnException = c.Boolean(nullable: false),
                        Kudos = c.Int(nullable: false),
                        ForumTotalUpvotes = c.Int(nullable: false),
                        ForumTotalDownvotes = c.Int(nullable: false),
                        VotesAvailable = c.Int(),
                        SteamID = c.Decimal(precision: 18, scale: 2),
                        SteamName = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.AccountID)
                .ForeignKey("dbo.Clan", t => t.ClanID)
                .ForeignKey("dbo.Faction", t => t.FactionID)
                .Index(t => t.ClanID)
                .Index(t => t.FactionID);
            
            CreateTable(
                "dbo.AccountBattleAward",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        SpringBattleID = c.Int(nullable: false),
                        AwardKey = c.String(nullable: false, maxLength: 50, unicode: false),
                        AwardDescription = c.String(maxLength: 500),
                        Value = c.Double(),
                    })
                .PrimaryKey(t => new { t.AccountID, t.SpringBattleID, t.AwardKey })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.SpringBattle", t => t.SpringBattleID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.SpringBattleID);
            
            CreateTable(
                "dbo.SpringBattle",
                c => new
                    {
                        SpringBattleID = c.Int(nullable: false, identity: true),
                        EngineGameID = c.String(maxLength: 64, unicode: false),
                        HostAccountID = c.Int(nullable: false),
                        Title = c.String(maxLength: 200),
                        MapResourceID = c.Int(nullable: false),
                        ModResourceID = c.Int(nullable: false),
                        StartTime = c.DateTime(nullable: false),
                        Duration = c.Int(nullable: false),
                        PlayerCount = c.Int(nullable: false),
                        HasBots = c.Boolean(nullable: false),
                        IsMission = c.Boolean(nullable: false),
                        ReplayFileName = c.String(maxLength: 500),
                        EngineVersion = c.String(maxLength: 100),
                        IsEloProcessed = c.Boolean(nullable: false),
                        WinnerTeamXpChange = c.Int(),
                        LoserTeamXpChange = c.Int(),
                        ForumThreadID = c.Int(),
                        TeamsTitle = c.String(maxLength: 250),
                        IsFfa = c.Boolean(nullable: false),
                        RatingPollID = c.Int(),
                    })
                .PrimaryKey(t => t.SpringBattleID)
                .ForeignKey("dbo.Account", t => t.HostAccountID)
                .ForeignKey("dbo.Resource", t => t.MapResourceID)
                .ForeignKey("dbo.Resource", t => t.ModResourceID)
                .ForeignKey("dbo.ForumThread", t => t.ForumThreadID)
                .ForeignKey("dbo.RatingPoll", t => t.RatingPollID)
                .Index(t => t.HostAccountID)
                .Index(t => t.MapResourceID)
                .Index(t => t.ModResourceID)
                .Index(t => t.ForumThreadID)
                .Index(t => t.RatingPollID);
            
            CreateTable(
                "dbo.Event",
                c => new
                    {
                        EventID = c.Int(nullable: false, identity: true),
                        Text = c.String(nullable: false, maxLength: 4000),
                        Time = c.DateTime(nullable: false),
                        Turn = c.Int(nullable: false),
                        PlainText = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.EventID);
            
            CreateTable(
                "dbo.Clan",
                c => new
                    {
                        ClanID = c.Int(nullable: false, identity: true),
                        ClanName = c.String(nullable: false, maxLength: 50, unicode: false),
                        Description = c.String(maxLength: 500, unicode: false),
                        Password = c.String(maxLength: 20, unicode: false),
                        SecretTopic = c.String(maxLength: 500),
                        Shortcut = c.String(nullable: false, maxLength: 6, unicode: false),
                        ForumThreadID = c.Int(),
                        IsDeleted = c.Boolean(nullable: false),
                        FactionID = c.Int(),
                    })
                .PrimaryKey(t => t.ClanID)
                .ForeignKey("dbo.Faction", t => t.FactionID)
                .ForeignKey("dbo.ForumThread", t => t.ForumThreadID)
                .Index(t => t.ForumThreadID)
                .Index(t => t.FactionID);
            
            CreateTable(
                "dbo.AccountRole",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        RoleTypeID = c.Int(nullable: false),
                        Inauguration = c.DateTime(nullable: false),
                        FactionID = c.Int(),
                        ClanID = c.Int(),
                    })
                .PrimaryKey(t => new { t.AccountID, t.RoleTypeID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Clan", t => t.ClanID)
                .ForeignKey("dbo.Faction", t => t.FactionID)
                .ForeignKey("dbo.RoleType", t => t.RoleTypeID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.RoleTypeID)
                .Index(t => t.FactionID)
                .Index(t => t.ClanID);
            
            CreateTable(
                "dbo.Faction",
                c => new
                    {
                        FactionID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 200),
                        Shortcut = c.String(nullable: false, maxLength: 50),
                        Color = c.String(nullable: false, maxLength: 20, unicode: false),
                        IsDeleted = c.Boolean(nullable: false),
                        Metal = c.Double(nullable: false),
                        Dropships = c.Double(nullable: false),
                        Bombers = c.Double(nullable: false),
                        SecretTopic = c.String(maxLength: 500),
                        EnergyProducedLastTurn = c.Double(nullable: false),
                        EnergyDemandLastTurn = c.Double(nullable: false),
                        Warps = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.FactionID);
            
            CreateTable(
                "dbo.FactionTreaty",
                c => new
                    {
                        FactionTreatyID = c.Int(nullable: false, identity: true),
                        ProposingFactionID = c.Int(nullable: false),
                        ProposingAccountID = c.Int(nullable: false),
                        AcceptingFactionID = c.Int(nullable: false),
                        AcceptedAccountID = c.Int(),
                        TurnsRemaining = c.Int(),
                        TreatyState = c.Int(nullable: false),
                        TurnsTotal = c.Int(),
                        TreatyNote = c.String(),
                    })
                .PrimaryKey(t => t.FactionTreatyID)
                .ForeignKey("dbo.Account", t => t.AcceptedAccountID)
                .ForeignKey("dbo.Account", t => t.ProposingAccountID)
                .ForeignKey("dbo.Faction", t => t.AcceptingFactionID)
                .ForeignKey("dbo.Faction", t => t.ProposingFactionID, cascadeDelete: true)
                .Index(t => t.ProposingFactionID)
                .Index(t => t.ProposingAccountID)
                .Index(t => t.AcceptingFactionID)
                .Index(t => t.AcceptedAccountID);
            
            CreateTable(
                "dbo.TreatyEffect",
                c => new
                    {
                        TreatyEffectID = c.Int(nullable: false, identity: true),
                        FactionTreatyID = c.Int(nullable: false),
                        EffectTypeID = c.Int(nullable: false),
                        GivingFactionID = c.Int(nullable: false),
                        ReceivingFactionID = c.Int(nullable: false),
                        Value = c.Double(),
                        PlanetID = c.Int(),
                    })
                .PrimaryKey(t => t.TreatyEffectID)
                .ForeignKey("dbo.Faction", t => t.GivingFactionID)
                .ForeignKey("dbo.Faction", t => t.ReceivingFactionID)
                .ForeignKey("dbo.FactionTreaty", t => t.FactionTreatyID, cascadeDelete: true)
                .ForeignKey("dbo.Planet", t => t.PlanetID)
                .ForeignKey("dbo.TreatyEffectType", t => t.EffectTypeID)
                .Index(t => t.FactionTreatyID)
                .Index(t => t.EffectTypeID)
                .Index(t => t.GivingFactionID)
                .Index(t => t.ReceivingFactionID)
                .Index(t => t.PlanetID);
            
            CreateTable(
                "dbo.Planet",
                c => new
                    {
                        PlanetID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50, unicode: false),
                        X = c.Double(nullable: false),
                        Y = c.Double(nullable: false),
                        MapResourceID = c.Int(),
                        OwnerAccountID = c.Int(),
                        GalaxyID = c.Int(nullable: false),
                        ForumThreadID = c.Int(),
                        OwnerFactionID = c.Int(),
                        TeamSize = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PlanetID)
                .ForeignKey("dbo.Account", t => t.OwnerAccountID)
                .ForeignKey("dbo.Faction", t => t.OwnerFactionID)
                .ForeignKey("dbo.ForumThread", t => t.ForumThreadID)
                .ForeignKey("dbo.Galaxy", t => t.GalaxyID, cascadeDelete: true)
                .ForeignKey("dbo.Resource", t => t.MapResourceID)
                .Index(t => t.MapResourceID)
                .Index(t => t.OwnerAccountID)
                .Index(t => t.GalaxyID)
                .Index(t => t.ForumThreadID)
                .Index(t => t.OwnerFactionID);
            
            CreateTable(
                "dbo.AccountPlanet",
                c => new
                    {
                        PlanetID = c.Int(nullable: false),
                        AccountID = c.Int(nullable: false),
                        AttackPoints = c.Double(nullable: false),
                    })
                .PrimaryKey(t => new { t.PlanetID, t.AccountID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Planet", t => t.PlanetID, cascadeDelete: true)
                .Index(t => t.PlanetID)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.ForumThread",
                c => new
                    {
                        ForumThreadID = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 300),
                        Created = c.DateTime(nullable: false),
                        CreatedAccountID = c.Int(),
                        LastPost = c.DateTime(),
                        LastPostAccountID = c.Int(),
                        PostCount = c.Int(nullable: false),
                        ViewCount = c.Int(nullable: false),
                        IsLocked = c.Boolean(nullable: false),
                        ForumCategoryID = c.Int(),
                        IsPinned = c.Boolean(nullable: false),
                        RestrictedClanID = c.Int(),
                    })
                .PrimaryKey(t => t.ForumThreadID)
                .ForeignKey("dbo.Account", t => t.CreatedAccountID)
                .ForeignKey("dbo.Account", t => t.LastPostAccountID)
                .ForeignKey("dbo.Clan", t => t.RestrictedClanID)
                .ForeignKey("dbo.ForumCategory", t => t.ForumCategoryID)
                .Index(t => t.CreatedAccountID)
                .Index(t => t.LastPostAccountID)
                .Index(t => t.ForumCategoryID)
                .Index(t => t.RestrictedClanID);
            
            CreateTable(
                "dbo.ForumCategory",
                c => new
                    {
                        ForumCategoryID = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 500),
                        ParentForumCategoryID = c.Int(),
                        IsLocked = c.Boolean(nullable: false),
                        IsMissions = c.Boolean(nullable: false),
                        IsMaps = c.Boolean(nullable: false),
                        SortOrder = c.Int(nullable: false),
                        IsSpringBattles = c.Boolean(nullable: false),
                        IsClans = c.Boolean(nullable: false),
                        IsPlanets = c.Boolean(nullable: false),
                        IsNews = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ForumCategoryID)
                .ForeignKey("dbo.ForumCategory", t => t.ParentForumCategoryID)
                .Index(t => t.ParentForumCategoryID);
            
            CreateTable(
                "dbo.ForumLastRead",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        ForumCategoryID = c.Int(nullable: false),
                        LastRead = c.DateTime(),
                    })
                .PrimaryKey(t => new { t.AccountID, t.ForumCategoryID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.ForumCategory", t => t.ForumCategoryID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.ForumCategoryID);
            
            CreateTable(
                "dbo.ForumPost",
                c => new
                    {
                        ForumPostID = c.Int(nullable: false, identity: true),
                        AuthorAccountID = c.Int(nullable: false),
                        Created = c.DateTime(nullable: false),
                        Text = c.String(nullable: false),
                        ForumThreadID = c.Int(nullable: false),
                        Upvotes = c.Int(nullable: false),
                        Downvotes = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ForumPostID)
                .ForeignKey("dbo.Account", t => t.AuthorAccountID)
                .ForeignKey("dbo.ForumThread", t => t.ForumThreadID, cascadeDelete: true)
                .Index(t => t.AuthorAccountID)
                .Index(t => t.ForumThreadID);
            
            CreateTable(
                "dbo.AccountForumVote",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        ForumPostID = c.Int(nullable: false),
                        Vote = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.ForumPostID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.ForumPost", t => t.ForumPostID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.ForumPostID);
            
            CreateTable(
                "dbo.ForumPostEdit",
                c => new
                    {
                        ForumPostEditID = c.Int(nullable: false, identity: true),
                        ForumPostID = c.Int(nullable: false),
                        EditorAccountID = c.Int(nullable: false),
                        OriginalText = c.String(),
                        NewText = c.String(),
                        EditTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ForumPostEditID)
                .ForeignKey("dbo.Account", t => t.EditorAccountID, cascadeDelete: true)
                .ForeignKey("dbo.ForumPost", t => t.ForumPostID, cascadeDelete: true)
                .Index(t => t.ForumPostID)
                .Index(t => t.EditorAccountID);
            
            CreateTable(
                "dbo.ForumThreadLastRead",
                c => new
                    {
                        ForumThreadID = c.Int(nullable: false),
                        AccountID = c.Int(nullable: false),
                        LastRead = c.DateTime(),
                        LastPosted = c.DateTime(),
                    })
                .PrimaryKey(t => new { t.ForumThreadID, t.AccountID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.ForumThread", t => t.ForumThreadID, cascadeDelete: true)
                .Index(t => t.ForumThreadID)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.Mission",
                c => new
                    {
                        MissionID = c.Int(nullable: false, identity: true),
                        AuthorName = c.String(),
                        Name = c.String(nullable: false, maxLength: 200),
                        Mod = c.String(maxLength: 100),
                        Map = c.String(maxLength: 100),
                        Mutator = c.Binary(),
                        Image = c.Binary(nullable: false),
                        Description = c.String(unicode: false),
                        DescriptionStory = c.String(unicode: false),
                        CreatedTime = c.DateTime(nullable: false),
                        ModifiedTime = c.DateTime(nullable: false),
                        ScoringMethod = c.String(maxLength: 500),
                        TopScoreLine = c.String(maxLength: 100),
                        MissionEditorVersion = c.String(maxLength: 20),
                        SpringVersion = c.String(maxLength: 100),
                        Revision = c.Int(nullable: false),
                        Script = c.String(nullable: false),
                        TokenCondition = c.String(maxLength: 500, unicode: false),
                        CampaignID = c.Int(),
                        AccountID = c.Int(nullable: false),
                        ModOptions = c.String(),
                        ModRapidTag = c.String(maxLength: 100),
                        MinHumans = c.Int(nullable: false),
                        MaxHumans = c.Int(nullable: false),
                        IsScriptMission = c.Boolean(nullable: false),
                        MissionRunCount = c.Int(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        ManualDependencies = c.String(),
                        Rating = c.Single(),
                        Difficulty = c.Single(),
                        IsCoop = c.Boolean(nullable: false),
                        ForumThreadID = c.Int(nullable: false),
                        FeaturedOrder = c.Single(),
                        RatingPollID = c.Int(),
                        DifficultyRatingPollID = c.Int(),
                        Mission1_MissionID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.MissionID)
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.ForumThread", t => t.ForumThreadID, cascadeDelete: true)
                .ForeignKey("dbo.Mission", t => t.Mission1_MissionID)
                .ForeignKey("dbo.RatingPoll", t => t.DifficultyRatingPollID)
                .ForeignKey("dbo.RatingPoll", t => t.RatingPollID)
                .Index(t => t.AccountID)
                .Index(t => t.ForumThreadID)
                .Index(t => t.RatingPollID)
                .Index(t => t.DifficultyRatingPollID)
                .Index(t => t.Mission1_MissionID);
            
            CreateTable(
                "dbo.CampaignPlanet",
                c => new
                    {
                        PlanetID = c.Int(nullable: false),
                        CampaignID = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 50, unicode: false),
                        MissionID = c.Int(nullable: false),
                        X = c.Double(nullable: false),
                        Y = c.Double(nullable: false),
                        IsSkirmish = c.Boolean(nullable: false),
                        Description = c.String(),
                        DescriptionStory = c.String(),
                        StartsUnlocked = c.Boolean(nullable: false),
                        HideIfLocked = c.Boolean(nullable: false),
                        DisplayedMap = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => new { t.PlanetID, t.CampaignID })
                .ForeignKey("dbo.Campaign", t => t.CampaignID, cascadeDelete: true)
                .ForeignKey("dbo.Mission", t => t.MissionID, cascadeDelete: true)
                .Index(t => t.CampaignID)
                .Index(t => t.MissionID);
            
            CreateTable(
                "dbo.AccountCampaignProgress",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        CampaignID = c.Int(nullable: false),
                        PlanetID = c.Int(nullable: false),
                        IsUnlocked = c.Boolean(nullable: false),
                        IsCompleted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.CampaignID, t.PlanetID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.CampaignPlanet", t => new { t.CampaignID, t.PlanetID })
                .ForeignKey("dbo.Campaign", t => t.CampaignID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => new { t.CampaignID, t.PlanetID });
            
            CreateTable(
                "dbo.Campaign",
                c => new
                    {
                        CampaignID = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 50),
                        Description = c.String(),
                        MapWidth = c.Int(nullable: false),
                        MapHeight = c.Int(nullable: false),
                        IsDirty = c.Boolean(nullable: false),
                        IsHidden = c.Boolean(),
                        MapImageName = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.CampaignID);
            
            CreateTable(
                "dbo.AccountCampaignJournalProgress",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        CampaignID = c.Int(nullable: false),
                        JournalID = c.Int(nullable: false),
                        IsUnlocked = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.CampaignID, t.JournalID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.CampaignJournal", t => new { t.CampaignID, t.JournalID }, cascadeDelete: true)
                .ForeignKey("dbo.Campaign", t => t.CampaignID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => new { t.CampaignID, t.JournalID });
            
            CreateTable(
                "dbo.CampaignJournal",
                c => new
                    {
                        CampaignID = c.Int(nullable: false),
                        JournalID = c.Int(nullable: false),
                        PlanetID = c.Int(nullable: false),
                        UnlockOnPlanetUnlock = c.Boolean(nullable: false),
                        UnlockOnPlanetCompletion = c.Boolean(nullable: false),
                        StartsUnlocked = c.Boolean(nullable: false),
                        Title = c.String(nullable: false, maxLength: 50),
                        Text = c.String(nullable: false),
                        Category = c.String(),
                    })
                .PrimaryKey(t => new { t.CampaignID, t.JournalID })
                .ForeignKey("dbo.Campaign", t => t.CampaignID)
                .ForeignKey("dbo.CampaignPlanet", t => new { t.CampaignID, t.PlanetID })
                .Index(t => t.CampaignID)
                .Index(t => new { t.CampaignID, t.PlanetID });
            
            CreateTable(
                "dbo.CampaignJournalVar",
                c => new
                    {
                        CampaignID = c.Int(nullable: false),
                        JournalID = c.Int(nullable: false),
                        RequiredVarID = c.Int(nullable: false),
                        RequiredValue = c.String(nullable: false),
                    })
                .PrimaryKey(t => new { t.CampaignID, t.JournalID, t.RequiredVarID })
                .ForeignKey("dbo.CampaignVar", t => new { t.CampaignID, t.RequiredVarID }, cascadeDelete: true)
                .ForeignKey("dbo.CampaignJournal", t => new { t.CampaignID, t.JournalID }, cascadeDelete: true)
                .ForeignKey("dbo.Campaign", t => t.CampaignID, cascadeDelete: true)
                .Index(t => new { t.CampaignID, t.RequiredVarID })
                .Index(t => new { t.CampaignID, t.JournalID });
            
            CreateTable(
                "dbo.CampaignVar",
                c => new
                    {
                        CampaignID = c.Int(nullable: false),
                        VarID = c.Int(nullable: false),
                        KeyString = c.String(nullable: false, maxLength: 50),
                        Description = c.String(),
                    })
                .PrimaryKey(t => new { t.CampaignID, t.VarID })
                .ForeignKey("dbo.Campaign", t => t.CampaignID)
                .Index(t => t.CampaignID);
            
            CreateTable(
                "dbo.AccountCampaignVar",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        CampaignID = c.Int(nullable: false),
                        VarID = c.Int(nullable: false),
                        Value = c.String(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.CampaignID, t.VarID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.CampaignVar", t => new { t.CampaignID, t.VarID }, cascadeDelete: true)
                .ForeignKey("dbo.Campaign", t => t.CampaignID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => new { t.CampaignID, t.VarID });
            
            CreateTable(
                "dbo.CampaignPlanetVar",
                c => new
                    {
                        CampaignID = c.Int(nullable: false),
                        PlanetID = c.Int(nullable: false),
                        RequiredVarID = c.Int(nullable: false),
                        RequiredValue = c.String(nullable: false),
                    })
                .PrimaryKey(t => new { t.CampaignID, t.PlanetID, t.RequiredVarID })
                .ForeignKey("dbo.Campaign", t => t.CampaignID)
                .ForeignKey("dbo.CampaignVar", t => new { t.CampaignID, t.RequiredVarID }, cascadeDelete: true)
                .ForeignKey("dbo.CampaignPlanet", t => new { t.PlanetID, t.CampaignID }, cascadeDelete: true)
                .Index(t => t.CampaignID)
                .Index(t => new { t.CampaignID, t.RequiredVarID })
                .Index(t => new { t.PlanetID, t.CampaignID });
            
            CreateTable(
                "dbo.CampaignEvent",
                c => new
                    {
                        EventID = c.Int(nullable: false, identity: true),
                        AccountID = c.Int(nullable: false),
                        CampaignID = c.Int(nullable: false),
                        PlanetID = c.Int(nullable: false),
                        Text = c.String(nullable: false, maxLength: 4000),
                        Time = c.DateTime(nullable: false),
                        PlainText = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.EventID)
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.CampaignPlanet", t => new { t.CampaignID, t.PlanetID })
                .ForeignKey("dbo.Campaign", t => t.CampaignID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => new { t.CampaignID, t.PlanetID });
            
            CreateTable(
                "dbo.CampaignLink",
                c => new
                    {
                        PlanetToUnlockID = c.Int(nullable: false),
                        UnlockingPlanetID = c.Int(nullable: false),
                        CampaignID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.PlanetToUnlockID, t.UnlockingPlanetID })
                .ForeignKey("dbo.Campaign", t => t.CampaignID, cascadeDelete: true)
                .ForeignKey("dbo.CampaignPlanet", t => new { t.PlanetToUnlockID, t.CampaignID }, cascadeDelete: true)
                .ForeignKey("dbo.CampaignPlanet", t => new { t.UnlockingPlanetID, t.CampaignID }, cascadeDelete: true)
                .Index(t => new { t.PlanetToUnlockID, t.CampaignID })
                .Index(t => new { t.UnlockingPlanetID, t.CampaignID })
                .Index(t => t.CampaignID);
            
            CreateTable(
                "dbo.MissionScore",
                c => new
                    {
                        MissionID = c.Int(nullable: false),
                        AccountID = c.Int(nullable: false),
                        Score = c.Int(nullable: false),
                        Time = c.DateTime(nullable: false),
                        MissionRevision = c.Int(nullable: false),
                        GameSeconds = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.MissionID, t.AccountID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Mission", t => t.MissionID, cascadeDelete: true)
                .Index(t => t.MissionID)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.RatingPoll",
                c => new
                    {
                        RatingPollID = c.Int(nullable: false, identity: true),
                        Average = c.Double(),
                        Count = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.RatingPollID);
            
            CreateTable(
                "dbo.AccountRatingVote",
                c => new
                    {
                        RatingPollID = c.Int(nullable: false),
                        AccountID = c.Int(nullable: false),
                        Vote = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.RatingPollID, t.AccountID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.RatingPoll", t => t.RatingPollID, cascadeDelete: true)
                .Index(t => t.RatingPollID)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.Resource",
                c => new
                    {
                        ResourceID = c.Int(nullable: false, identity: true),
                        InternalName = c.String(nullable: false, maxLength: 255),
                        TypeID = c.Int(nullable: false),
                        LastLinkCheck = c.DateTime(),
                        DownloadCount = c.Int(nullable: false),
                        NoLinkDownloadCount = c.Int(nullable: false),
                        MissionID = c.Int(),
                        LastChange = c.DateTime(),
                        AuthorName = c.String(maxLength: 200),
                        MapTags = c.String(),
                        MapWidth = c.Int(),
                        MapHeight = c.Int(),
                        MapSizeSquared = c.Int(),
                        MapSizeRatio = c.Single(),
                        MapIsSupported = c.Boolean(),
                        MapIsAssymetrical = c.Boolean(),
                        MapHills = c.Int(),
                        MapWaterLevel = c.Int(),
                        MapIs1v1 = c.Boolean(),
                        MapIsTeams = c.Boolean(),
                        MapIsFfa = c.Boolean(),
                        MapIsChickens = c.Boolean(),
                        MapIsSpecial = c.Boolean(),
                        MapFFAMaxTeams = c.Int(),
                        MapRatingCount = c.Int(),
                        MapRatingSum = c.Int(),
                        TaggedByAccountID = c.Int(),
                        ForumThreadID = c.Int(),
                        FeaturedOrder = c.Single(),
                        MapPlanetWarsIcon = c.String(maxLength: 50),
                        RatingPollID = c.Int(),
                        MapSpringieCommands = c.String(maxLength: 2000),
                    })
                .PrimaryKey(t => t.ResourceID)
                .ForeignKey("dbo.Account", t => t.TaggedByAccountID)
                .ForeignKey("dbo.ForumThread", t => t.ForumThreadID)
                .ForeignKey("dbo.Mission", t => t.MissionID)
                .ForeignKey("dbo.RatingPoll", t => t.RatingPollID)
                .Index(t => t.MissionID)
                .Index(t => t.TaggedByAccountID)
                .Index(t => t.ForumThreadID)
                .Index(t => t.RatingPollID);
            
            CreateTable(
                "dbo.MapRating",
                c => new
                    {
                        ResourceID = c.Int(nullable: false),
                        AccountID = c.Int(nullable: false),
                        Rating = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ResourceID, t.AccountID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Resource", t => t.ResourceID, cascadeDelete: true)
                .Index(t => t.ResourceID)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.ResourceContentFile",
                c => new
                    {
                        ResourceID = c.Int(nullable: false),
                        Md5 = c.String(nullable: false, maxLength: 32, fixedLength: true, unicode: false),
                        Length = c.Int(nullable: false),
                        FileName = c.String(nullable: false, maxLength: 255),
                        Links = c.String(unicode: false),
                        LinkCount = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ResourceID, t.Md5 })
                .ForeignKey("dbo.Resource", t => t.ResourceID, cascadeDelete: true)
                .Index(t => t.ResourceID);
            
            CreateTable(
                "dbo.ResourceDependency",
                c => new
                    {
                        ResourceID = c.Int(nullable: false),
                        NeedsInternalName = c.String(nullable: false, maxLength: 250, unicode: false),
                    })
                .PrimaryKey(t => new { t.ResourceID, t.NeedsInternalName })
                .ForeignKey("dbo.Resource", t => t.ResourceID, cascadeDelete: true)
                .Index(t => t.ResourceID);
            
            CreateTable(
                "dbo.ResourceSpringHash",
                c => new
                    {
                        ResourceID = c.Int(nullable: false),
                        SpringVersion = c.String(nullable: false, maxLength: 50),
                        SpringHash = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ResourceID, t.SpringVersion })
                .ForeignKey("dbo.Resource", t => t.ResourceID, cascadeDelete: true)
                .Index(t => t.ResourceID);
            
            CreateTable(
                "dbo.Rating",
                c => new
                    {
                        RatingID = c.Int(nullable: false, identity: true),
                        AccountID = c.Int(nullable: false),
                        MissionID = c.Int(),
                        Rating = c.Int(),
                        Difficulty = c.Int(),
                    })
                .PrimaryKey(t => t.RatingID)
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Mission", t => t.MissionID)
                .Index(t => t.AccountID)
                .Index(t => t.MissionID);
            
            CreateTable(
                "dbo.News",
                c => new
                    {
                        NewsID = c.Int(nullable: false, identity: true),
                        Created = c.DateTime(nullable: false),
                        Title = c.String(nullable: false, maxLength: 200),
                        Text = c.String(nullable: false, unicode: false),
                        AuthorAccountID = c.Int(nullable: false),
                        HeadlineUntil = c.DateTime(nullable: false),
                        ForumThreadID = c.Int(nullable: false),
                        SpringForumPostID = c.Int(),
                        ImageExtension = c.String(maxLength: 50),
                        ImageContentType = c.String(maxLength: 50),
                        ImageLength = c.Int(),
                    })
                .PrimaryKey(t => t.NewsID)
                .ForeignKey("dbo.Account", t => t.AuthorAccountID, cascadeDelete: true)
                .ForeignKey("dbo.ForumThread", t => t.ForumThreadID, cascadeDelete: true)
                .Index(t => t.AuthorAccountID)
                .Index(t => t.ForumThreadID);
            
            CreateTable(
                "dbo.Galaxy",
                c => new
                    {
                        GalaxyID = c.Int(nullable: false, identity: true),
                        Started = c.DateTime(),
                        Turn = c.Int(nullable: false),
                        TurnStarted = c.DateTime(),
                        ImageName = c.String(maxLength: 100),
                        IsDirty = c.Boolean(nullable: false),
                        Width = c.Int(nullable: false),
                        Height = c.Int(nullable: false),
                        IsDefault = c.Boolean(nullable: false),
                        AttackerSideCounter = c.Int(nullable: false),
                        AttackerSideChangeTime = c.DateTime(),
                        MatchMakerState = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.GalaxyID);
            
            CreateTable(
                "dbo.Link",
                c => new
                    {
                        PlanetID1 = c.Int(nullable: false),
                        PlanetID2 = c.Int(nullable: false),
                        GalaxyID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.PlanetID1, t.PlanetID2 })
                .ForeignKey("dbo.Galaxy", t => t.GalaxyID, cascadeDelete: true)
                .ForeignKey("dbo.Planet", t => t.PlanetID1, cascadeDelete: true)
                .ForeignKey("dbo.Planet", t => t.PlanetID2, cascadeDelete: true)
                .Index(t => t.PlanetID1)
                .Index(t => t.PlanetID2)
                .Index(t => t.GalaxyID);
            
            CreateTable(
                "dbo.MarketOffer",
                c => new
                    {
                        OfferID = c.Int(nullable: false, identity: true),
                        AccountID = c.Int(nullable: false),
                        PlanetID = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        Price = c.Int(nullable: false),
                        IsSell = c.Boolean(nullable: false),
                        DatePlaced = c.DateTime(),
                        DateAccepted = c.DateTime(),
                        AcceptedAccountID = c.Int(),
                    })
                .PrimaryKey(t => t.OfferID)
                .ForeignKey("dbo.Account", t => t.AcceptedAccountID)
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Planet", t => t.PlanetID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.PlanetID)
                .Index(t => t.AcceptedAccountID);
            
            CreateTable(
                "dbo.PlanetFaction",
                c => new
                    {
                        PlanetID = c.Int(nullable: false),
                        FactionID = c.Int(nullable: false),
                        Influence = c.Double(nullable: false),
                        Dropships = c.Int(nullable: false),
                        DropshipsLastAdded = c.DateTime(),
                    })
                .PrimaryKey(t => new { t.PlanetID, t.FactionID })
                .ForeignKey("dbo.Faction", t => t.FactionID, cascadeDelete: true)
                .ForeignKey("dbo.Planet", t => t.PlanetID, cascadeDelete: true)
                .Index(t => t.PlanetID)
                .Index(t => t.FactionID);
            
            CreateTable(
                "dbo.PlanetOwnerHistory",
                c => new
                    {
                        PlanetID = c.Int(nullable: false),
                        Turn = c.Int(nullable: false),
                        OwnerAccountID = c.Int(),
                        OwnerClanID = c.Int(),
                        OwnerFactionID = c.Int(),
                    })
                .PrimaryKey(t => new { t.PlanetID, t.Turn })
                .ForeignKey("dbo.Account", t => t.OwnerAccountID)
                .ForeignKey("dbo.Clan", t => t.OwnerClanID)
                .ForeignKey("dbo.Faction", t => t.OwnerFactionID)
                .ForeignKey("dbo.Planet", t => t.PlanetID, cascadeDelete: true)
                .Index(t => t.PlanetID)
                .Index(t => t.OwnerAccountID)
                .Index(t => t.OwnerClanID)
                .Index(t => t.OwnerFactionID);
            
            CreateTable(
                "dbo.PlanetStructure",
                c => new
                    {
                        PlanetID = c.Int(nullable: false),
                        StructureTypeID = c.Int(nullable: false),
                        OwnerAccountID = c.Int(),
                        ActivatedOnTurn = c.Int(),
                        EnergyPriority = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        TargetPlanetID = c.Int(),
                    })
                .PrimaryKey(t => new { t.PlanetID, t.StructureTypeID })
                .ForeignKey("dbo.Account", t => t.OwnerAccountID)
                .ForeignKey("dbo.Planet", t => t.PlanetID, cascadeDelete: true)
                .ForeignKey("dbo.Planet", t => t.TargetPlanetID)
                .ForeignKey("dbo.StructureType", t => t.StructureTypeID, cascadeDelete: true)
                .Index(t => t.PlanetID)
                .Index(t => t.StructureTypeID)
                .Index(t => t.OwnerAccountID)
                .Index(t => t.TargetPlanetID);
            
            CreateTable(
                "dbo.StructureType",
                c => new
                    {
                        StructureTypeID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 250),
                        IngameUnitName = c.String(maxLength: 50),
                        MapIcon = c.String(maxLength: 50),
                        DisabledMapIcon = c.String(maxLength: 50),
                        UpkeepEnergy = c.Double(),
                        TurnsToActivate = c.Int(),
                        EffectDropshipProduction = c.Double(),
                        EffectDropshipCapacity = c.Int(),
                        EffectBomberProduction = c.Double(),
                        EffectBomberCapacity = c.Int(),
                        EffectInfluenceSpread = c.Double(),
                        EffectUnlockID = c.Int(),
                        EffectEnergyPerTurn = c.Double(),
                        EffectIsVictoryPlanet = c.Boolean(),
                        EffectWarpProduction = c.Double(),
                        EffectAllowShipTraversal = c.Boolean(),
                        EffectDropshipDefense = c.Double(),
                        EffectBomberDefense = c.Double(),
                        EffectBots = c.String(maxLength: 100),
                        EffectBlocksInfluenceSpread = c.Boolean(),
                        EffectBlocksJumpgate = c.Boolean(),
                        EffectRemoteInfluenceSpread = c.Double(),
                        EffectCreateLink = c.Boolean(),
                        EffectChangePlanetMap = c.Boolean(),
                        EffectPlanetBuster = c.Boolean(),
                        Cost = c.Double(nullable: false),
                        IsBuildable = c.Boolean(nullable: false),
                        IsIngameDestructible = c.Boolean(nullable: false),
                        IsBomberDestructible = c.Boolean(nullable: false),
                        OwnerChangeDeletesThis = c.Boolean(nullable: false),
                        OwnerChangeDisablesThis = c.Boolean(nullable: false),
                        BattleDeletesThis = c.Boolean(nullable: false),
                        IsSingleUse = c.Boolean(nullable: false),
                        RequiresPlanetTarget = c.Boolean(nullable: false),
                        EffectReduceBattleInfluenceGain = c.Double(),
                    })
                .PrimaryKey(t => t.StructureTypeID)
                .ForeignKey("dbo.Unlock", t => t.EffectUnlockID)
                .Index(t => t.EffectUnlockID);
            
            CreateTable(
                "dbo.Unlock",
                c => new
                    {
                        UnlockID = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 100),
                        Name = c.String(maxLength: 200),
                        Description = c.String(maxLength: 1000),
                        NeededLevel = c.Int(nullable: false),
                        LimitForChassis = c.String(maxLength: 500),
                        UnlockType = c.Int(nullable: false),
                        RequiredUnlockID = c.Int(),
                        MorphLevel = c.Int(nullable: false),
                        MaxModuleCount = c.Int(nullable: false),
                        MetalCost = c.Int(),
                        XpCost = c.Int(nullable: false),
                        MetalCostMorph2 = c.Int(),
                        MetalCostMorph3 = c.Int(),
                        MetalCostMorph4 = c.Int(),
                        MetalCostMorph5 = c.Int(),
                        KudosCost = c.Int(),
                        IsKudosOnly = c.Boolean(),
                    })
                .PrimaryKey(t => t.UnlockID)
                .ForeignKey("dbo.Unlock", t => t.RequiredUnlockID)
                .Index(t => t.RequiredUnlockID);
            
            CreateTable(
                "dbo.AccountUnlock",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        UnlockID = c.Int(nullable: false),
                        Count = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.UnlockID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Unlock", t => t.UnlockID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.UnlockID);
            
            CreateTable(
                "dbo.CommanderDecorationIcon",
                c => new
                    {
                        DecorationUnlockID = c.Int(nullable: false),
                        IconPosition = c.Int(nullable: false),
                        IconType = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.DecorationUnlockID)
                .ForeignKey("dbo.Unlock", t => t.DecorationUnlockID)
                .Index(t => t.DecorationUnlockID);
            
            CreateTable(
                "dbo.CommanderDecoration",
                c => new
                    {
                        CommanderID = c.Int(nullable: false),
                        SlotID = c.Int(nullable: false),
                        DecorationUnlockID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CommanderID, t.SlotID })
                .ForeignKey("dbo.Commander", t => t.CommanderID, cascadeDelete: true)
                .ForeignKey("dbo.CommanderDecorationSlot", t => t.SlotID, cascadeDelete: true)
                .ForeignKey("dbo.Unlock", t => t.DecorationUnlockID, cascadeDelete: true)
                .Index(t => t.CommanderID)
                .Index(t => t.SlotID)
                .Index(t => t.DecorationUnlockID);
            
            CreateTable(
                "dbo.Commander",
                c => new
                    {
                        CommanderID = c.Int(nullable: false, identity: true),
                        AccountID = c.Int(nullable: false),
                        ProfileNumber = c.Int(nullable: false),
                        Name = c.String(maxLength: 200),
                        ChassisUnlockID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CommanderID)
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Unlock", t => t.ChassisUnlockID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.ChassisUnlockID);
            
            CreateTable(
                "dbo.CommanderModule",
                c => new
                    {
                        CommanderID = c.Int(nullable: false),
                        SlotID = c.Int(nullable: false),
                        ModuleUnlockID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.CommanderID, t.SlotID })
                .ForeignKey("dbo.Commander", t => t.CommanderID, cascadeDelete: true)
                .ForeignKey("dbo.CommanderSlot", t => t.SlotID, cascadeDelete: true)
                .ForeignKey("dbo.Unlock", t => t.ModuleUnlockID, cascadeDelete: true)
                .Index(t => t.CommanderID)
                .Index(t => t.SlotID)
                .Index(t => t.ModuleUnlockID);
            
            CreateTable(
                "dbo.CommanderSlot",
                c => new
                    {
                        CommanderSlotID = c.Int(nullable: false, identity: true),
                        MorphLevel = c.Int(nullable: false),
                        UnlockType = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CommanderSlotID);
            
            CreateTable(
                "dbo.CommanderDecorationSlot",
                c => new
                    {
                        CommanderDecorationSlotID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CommanderDecorationSlotID);
            
            CreateTable(
                "dbo.KudosPurchase",
                c => new
                    {
                        KudosPurchaseID = c.Int(nullable: false, identity: true),
                        AccountID = c.Int(nullable: false),
                        KudosValue = c.Int(nullable: false),
                        Time = c.DateTime(nullable: false),
                        UnlockID = c.Int(),
                    })
                .PrimaryKey(t => t.KudosPurchaseID)
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Unlock", t => t.UnlockID)
                .Index(t => t.AccountID)
                .Index(t => t.UnlockID);
            
            CreateTable(
                "dbo.TreatyEffectType",
                c => new
                    {
                        EffectTypeID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Description = c.String(nullable: false, maxLength: 200),
                        HasValue = c.Boolean(nullable: false),
                        MinValue = c.Double(),
                        MaxValue = c.Double(),
                        IsPlanetBased = c.Boolean(nullable: false),
                        IsOneTimeOnly = c.Boolean(nullable: false),
                        EffectBalanceSameSide = c.Boolean(),
                        EffectPreventInfluenceSpread = c.Boolean(),
                        EffectPreventDropshipAttack = c.Boolean(),
                        EffectPreventBomberAttack = c.Boolean(),
                        EffectAllowDropshipPass = c.Boolean(),
                        EffectAllowBomberPass = c.Boolean(),
                        EffectGiveMetal = c.Boolean(),
                        EffectGiveDropships = c.Boolean(),
                        EffectGiveBombers = c.Boolean(),
                        EffectGiveEnergy = c.Boolean(),
                        EffectShareTechs = c.Boolean(),
                        EffectGiveWarps = c.Boolean(),
                        EffectPreventIngamePwStructureDestruction = c.Boolean(),
                        EffectGiveInfluence = c.Boolean(),
                    })
                .PrimaryKey(t => t.EffectTypeID);
            
            CreateTable(
                "dbo.Poll",
                c => new
                    {
                        PollID = c.Int(nullable: false, identity: true),
                        QuestionText = c.String(nullable: false, maxLength: 500),
                        IsAnonymous = c.Boolean(nullable: false),
                        RoleTypeID = c.Int(),
                        RoleTargetAccountID = c.Int(),
                        RoleIsRemoval = c.Boolean(nullable: false),
                        RestrictFactionID = c.Int(),
                        RestrictClanID = c.Int(),
                        CreatedAccountID = c.Int(),
                        ExpireBy = c.DateTime(),
                        IsHeadline = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.PollID)
                .ForeignKey("dbo.Account", t => t.CreatedAccountID)
                .ForeignKey("dbo.Account", t => t.RoleTargetAccountID)
                .ForeignKey("dbo.Clan", t => t.RestrictClanID)
                .ForeignKey("dbo.Faction", t => t.RestrictFactionID)
                .ForeignKey("dbo.RoleType", t => t.RoleTypeID)
                .Index(t => t.RoleTypeID)
                .Index(t => t.RoleTargetAccountID)
                .Index(t => t.RestrictFactionID)
                .Index(t => t.RestrictClanID)
                .Index(t => t.CreatedAccountID);
            
            CreateTable(
                "dbo.PollOption",
                c => new
                    {
                        OptionID = c.Int(nullable: false, identity: true),
                        PollID = c.Int(nullable: false),
                        OptionText = c.String(nullable: false, maxLength: 200),
                        Votes = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.OptionID)
                .ForeignKey("dbo.Poll", t => t.PollID, cascadeDelete: true)
                .Index(t => t.PollID);
            
            CreateTable(
                "dbo.PollVote",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        PollID = c.Int(nullable: false),
                        OptionID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.PollID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Poll", t => t.PollID, cascadeDelete: true)
                .ForeignKey("dbo.PollOption", t => t.OptionID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.PollID)
                .Index(t => t.OptionID);
            
            CreateTable(
                "dbo.RoleType",
                c => new
                    {
                        RoleTypeID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Description = c.String(nullable: false, maxLength: 500),
                        IsClanOnly = c.Boolean(nullable: false),
                        IsOnePersonOnly = c.Boolean(nullable: false),
                        RestrictFactionID = c.Int(),
                        IsVoteable = c.Boolean(nullable: false),
                        PollDurationDays = c.Int(nullable: false),
                        RightDropshipQuota = c.Double(nullable: false),
                        RightBomberQuota = c.Double(nullable: false),
                        RightMetalQuota = c.Double(nullable: false),
                        RightWarpQuota = c.Double(nullable: false),
                        RightDiplomacy = c.Boolean(nullable: false),
                        RightEditTexts = c.Boolean(nullable: false),
                        RightSetEnergyPriority = c.Boolean(nullable: false),
                        RightKickPeople = c.Boolean(nullable: false),
                        DisplayOrder = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.RoleTypeID)
                .ForeignKey("dbo.Faction", t => t.RestrictFactionID)
                .Index(t => t.RestrictFactionID);
            
            CreateTable(
                "dbo.RoleTypeHierarchy",
                c => new
                    {
                        MasterRoleTypeID = c.Int(nullable: false),
                        SlaveRoleTypeID = c.Int(nullable: false),
                        CanAppoint = c.Boolean(nullable: false),
                        CanRecall = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.MasterRoleTypeID, t.SlaveRoleTypeID })
                .ForeignKey("dbo.RoleType", t => t.MasterRoleTypeID, cascadeDelete: true)
                .ForeignKey("dbo.RoleType", t => t.SlaveRoleTypeID, cascadeDelete: true)
                .Index(t => t.MasterRoleTypeID)
                .Index(t => t.SlaveRoleTypeID);
            
            CreateTable(
                "dbo.SpringBattlePlayer",
                c => new
                    {
                        SpringBattleID = c.Int(nullable: false),
                        AccountID = c.Int(nullable: false),
                        IsSpectator = c.Boolean(nullable: false),
                        IsInVictoryTeam = c.Boolean(nullable: false),
                        CommanderType = c.String(maxLength: 50),
                        LoseTime = c.Int(),
                        AllyNumber = c.Int(nullable: false),
                        Rank = c.Int(nullable: false),
                        EloChange = c.Single(),
                        XpChange = c.Int(),
                        Influence = c.Int(),
                    })
                .PrimaryKey(t => new { t.SpringBattleID, t.AccountID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.SpringBattle", t => t.SpringBattleID, cascadeDelete: true)
                .Index(t => t.SpringBattleID)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.AccountIP",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        IP = c.String(nullable: false, maxLength: 50),
                        LoginCount = c.Int(nullable: false),
                        FirstLogin = c.DateTime(nullable: false),
                        LastLogin = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.IP })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.AccountUserID",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        UserID = c.Long(nullable: false),
                        LoginCount = c.Int(nullable: false),
                        FirstLogin = c.DateTime(nullable: false),
                        LastLogin = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.UserID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.AutoBanSmurfList",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        BanLobby = c.Boolean(nullable: false),
                        BanSite = c.Boolean(nullable: false),
                        BanIP = c.Boolean(nullable: false),
                        BanUserID = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.AccountID)
                .ForeignKey("dbo.Account", t => t.AccountID)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.ContributionJar",
                c => new
                    {
                        ContributionJarID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        GuarantorAccountID = c.Int(nullable: false),
                        Description = c.String(maxLength: 500),
                        TargetGrossEuros = c.Double(nullable: false),
                        IsDefault = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ContributionJarID)
                .ForeignKey("dbo.Account", t => t.GuarantorAccountID, cascadeDelete: true)
                .Index(t => t.GuarantorAccountID);
            
            CreateTable(
                "dbo.Contribution",
                c => new
                    {
                        ContributionID = c.Int(nullable: false, identity: true),
                        AccountID = c.Int(),
                        Time = c.DateTime(nullable: false),
                        PayPalTransactionID = c.String(maxLength: 50),
                        Name = c.String(maxLength: 100),
                        OriginalCurrency = c.String(maxLength: 5),
                        OriginalAmount = c.Double(),
                        Euros = c.Double(),
                        EurosNet = c.Double(),
                        KudosValue = c.Int(nullable: false),
                        ItemName = c.String(maxLength: 50),
                        ItemCode = c.String(maxLength: 50),
                        Email = c.String(maxLength: 50),
                        Comment = c.String(maxLength: 200),
                        PackID = c.Int(),
                        RedeemCode = c.String(maxLength: 100),
                        IsSpringContribution = c.Boolean(nullable: false),
                        ManuallyAddedAccountID = c.Int(),
                        ContributionJarID = c.Int(),
                    })
                .PrimaryKey(t => t.ContributionID)
                .ForeignKey("dbo.Account", t => t.ManuallyAddedAccountID)
                .ForeignKey("dbo.Account", t => t.AccountID)
                .ForeignKey("dbo.ContributionJar", t => t.ContributionJarID)
                .Index(t => t.AccountID)
                .Index(t => t.ManuallyAddedAccountID)
                .Index(t => t.ContributionJarID);
            
            CreateTable(
                "dbo.LobbyChannelSubscription",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        Channel = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => new { t.AccountID, t.Channel })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .Index(t => t.AccountID);
            
            CreateTable(
                "dbo.Punishment",
                c => new
                    {
                        PunishmentID = c.Int(nullable: false, identity: true),
                        AccountID = c.Int(nullable: false),
                        Reason = c.String(nullable: false, maxLength: 1000),
                        Time = c.DateTime(nullable: false),
                        BanExpires = c.DateTime(),
                        BanMute = c.Boolean(nullable: false),
                        BanCommanders = c.Boolean(nullable: false),
                        BanUnlocks = c.Boolean(nullable: false),
                        BanSite = c.Boolean(nullable: false),
                        BanLobby = c.Boolean(nullable: false),
                        BanIP = c.String(maxLength: 1000),
                        BanForum = c.Boolean(nullable: false),
                        UserID = c.Long(),
                        CreatedAccountID = c.Int(),
                        DeleteInfluence = c.Boolean(nullable: false),
                        DeleteXP = c.Boolean(nullable: false),
                        SegregateHost = c.Boolean(nullable: false),
                        SetRightsToZero = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.PunishmentID)
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Account", t => t.CreatedAccountID)
                .Index(t => t.AccountID)
                .Index(t => t.CreatedAccountID);
            
            CreateTable(
                "dbo.AutohostConfig",
                c => new
                    {
                        AutohostConfigID = c.Int(nullable: false, identity: true),
                        ClusterNode = c.String(nullable: false, maxLength: 50),
                        Login = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 50),
                        MaxPlayers = c.Int(nullable: false),
                        Welcome = c.String(maxLength: 200),
                        AutoSpawn = c.Boolean(nullable: false),
                        AutoUpdateRapidTag = c.String(maxLength: 50),
                        SpringVersion = c.String(maxLength: 50),
                        AutoUpdateSpringBranch = c.String(maxLength: 50),
                        CommandLevels = c.String(maxLength: 500),
                        Map = c.String(maxLength: 100),
                        Mod = c.String(maxLength: 100),
                        Title = c.String(maxLength: 100),
                        JoinChannels = c.String(maxLength: 100),
                        BattlePassword = c.String(maxLength: 50),
                        AutohostMode = c.Int(nullable: false),
                        MinToStart = c.Int(),
                        MaxToStart = c.Int(),
                        MinToJuggle = c.Int(),
                        MaxToJuggle = c.Int(),
                        SplitBiggerThan = c.Int(),
                        MergeSmallerThan = c.Int(),
                        MaxEloDifference = c.Int(),
                        DontMoveManuallyJoined = c.Boolean(),
                        MinLevel = c.Int(),
                        MinElo = c.Int(),
                        MaxLevel = c.Int(),
                        MaxElo = c.Int(),
                        IsTrollHost = c.Boolean(),
                    })
                .PrimaryKey(t => t.AutohostConfigID);
            
            CreateTable(
                "dbo.Avatar",
                c => new
                    {
                        AvatarName = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.AvatarName);
            
            CreateTable(
                "dbo.BlockedCompany",
                c => new
                    {
                        CompanyID = c.Int(nullable: false, identity: true),
                        CompanyName = c.String(nullable: false),
                        Comment = c.String(),
                    })
                .PrimaryKey(t => t.CompanyID);
            
            CreateTable(
                "dbo.BlockedHost",
                c => new
                    {
                        HostID = c.Int(nullable: false, identity: true),
                        HostName = c.String(nullable: false),
                        Comment = c.String(),
                    })
                .PrimaryKey(t => t.HostID);
            
            CreateTable(
                "dbo.ExceptionLog",
                c => new
                    {
                        ExceptionLogID = c.Int(nullable: false, identity: true),
                        ProgramID = c.Int(nullable: false),
                        Exception = c.String(nullable: false),
                        ExtraData = c.String(),
                        RemoteIP = c.String(maxLength: 50),
                        PlayerName = c.String(maxLength: 200),
                        Time = c.DateTime(nullable: false),
                        ProgramVersion = c.String(maxLength: 100),
                        ExceptionHash = c.String(nullable: false, maxLength: 32, fixedLength: true, unicode: false),
                    })
                .PrimaryKey(t => t.ExceptionLogID);
            
            CreateTable(
                "dbo.LobbyMessage",
                c => new
                    {
                        MessageID = c.Int(nullable: false, identity: true),
                        SourceName = c.String(nullable: false, maxLength: 200),
                        TargetName = c.String(nullable: false, maxLength: 200),
                        SourceLobbyID = c.Int(),
                        Message = c.String(maxLength: 2000),
                        Created = c.DateTime(nullable: false),
                        TargetLobbyID = c.Int(),
                        Channel = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.MessageID);
            
            CreateTable(
                "dbo.MiscVar",
                c => new
                    {
                        VarName = c.String(nullable: false, maxLength: 128),
                        VarValue = c.String(),
                    })
                .PrimaryKey(t => t.VarName);
            
            CreateTable(
                "dbo.EventClan",
                c => new
                    {
                        ClanID = c.Int(nullable: false),
                        EventID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ClanID, t.EventID })
                .ForeignKey("dbo.Clan", t => t.ClanID, cascadeDelete: true)
                .ForeignKey("dbo.Event", t => t.EventID, cascadeDelete: true)
                .Index(t => t.ClanID)
                .Index(t => t.EventID);
            
            CreateTable(
                "dbo.EventFaction",
                c => new
                    {
                        EventID = c.Int(nullable: false),
                        FactionID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.EventID, t.FactionID })
                .ForeignKey("dbo.Event", t => t.EventID, cascadeDelete: true)
                .ForeignKey("dbo.Faction", t => t.FactionID, cascadeDelete: true)
                .Index(t => t.EventID)
                .Index(t => t.FactionID);
            
            CreateTable(
                "dbo.EventPlanet",
                c => new
                    {
                        EventID = c.Int(nullable: false),
                        PlanetID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.EventID, t.PlanetID })
                .ForeignKey("dbo.Event", t => t.EventID, cascadeDelete: true)
                .ForeignKey("dbo.Planet", t => t.PlanetID, cascadeDelete: true)
                .Index(t => t.EventID)
                .Index(t => t.PlanetID);
            
            CreateTable(
                "dbo.EventSpringBattle",
                c => new
                    {
                        EventID = c.Int(nullable: false),
                        SpringBattleID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.EventID, t.SpringBattleID })
                .ForeignKey("dbo.Event", t => t.EventID, cascadeDelete: true)
                .ForeignKey("dbo.SpringBattle", t => t.SpringBattleID, cascadeDelete: true)
                .Index(t => t.EventID)
                .Index(t => t.SpringBattleID);
            
            CreateTable(
                "dbo.EventAccount",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        EventID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.EventID })
                .ForeignKey("dbo.Account", t => t.AccountID, cascadeDelete: true)
                .ForeignKey("dbo.Event", t => t.EventID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.EventID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AbuseReport", "ReporterAccountID", "dbo.Account");
            DropForeignKey("dbo.AbuseReport", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Punishment", "CreatedAccountID", "dbo.Account");
            DropForeignKey("dbo.Punishment", "AccountID", "dbo.Account");
            DropForeignKey("dbo.LobbyChannelSubscription", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Account", "FactionID", "dbo.Faction");
            DropForeignKey("dbo.EventAccount", "EventID", "dbo.Event");
            DropForeignKey("dbo.EventAccount", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Contribution", "ContributionJarID", "dbo.ContributionJar");
            DropForeignKey("dbo.Contribution", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Contribution", "ManuallyAddedAccountID", "dbo.Account");
            DropForeignKey("dbo.ContributionJar", "GuarantorAccountID", "dbo.Account");
            DropForeignKey("dbo.AutoBanSmurfList", "AccountID", "dbo.Account");
            DropForeignKey("dbo.AccountUserID", "AccountID", "dbo.Account");
            DropForeignKey("dbo.AccountIP", "AccountID", "dbo.Account");
            DropForeignKey("dbo.AccountBattleAward", "SpringBattleID", "dbo.SpringBattle");
            DropForeignKey("dbo.SpringBattlePlayer", "SpringBattleID", "dbo.SpringBattle");
            DropForeignKey("dbo.SpringBattlePlayer", "AccountID", "dbo.Account");
            DropForeignKey("dbo.SpringBattle", "RatingPollID", "dbo.RatingPoll");
            DropForeignKey("dbo.SpringBattle", "ForumThreadID", "dbo.ForumThread");
            DropForeignKey("dbo.EventSpringBattle", "SpringBattleID", "dbo.SpringBattle");
            DropForeignKey("dbo.EventSpringBattle", "EventID", "dbo.Event");
            DropForeignKey("dbo.EventPlanet", "PlanetID", "dbo.Planet");
            DropForeignKey("dbo.EventPlanet", "EventID", "dbo.Event");
            DropForeignKey("dbo.EventFaction", "FactionID", "dbo.Faction");
            DropForeignKey("dbo.EventFaction", "EventID", "dbo.Event");
            DropForeignKey("dbo.Clan", "ForumThreadID", "dbo.ForumThread");
            DropForeignKey("dbo.Clan", "FactionID", "dbo.Faction");
            DropForeignKey("dbo.EventClan", "EventID", "dbo.Event");
            DropForeignKey("dbo.EventClan", "ClanID", "dbo.Clan");
            DropForeignKey("dbo.Account", "ClanID", "dbo.Clan");
            DropForeignKey("dbo.AccountRole", "RoleTypeID", "dbo.RoleType");
            DropForeignKey("dbo.AccountRole", "FactionID", "dbo.Faction");
            DropForeignKey("dbo.Poll", "RoleTypeID", "dbo.RoleType");
            DropForeignKey("dbo.RoleTypeHierarchy", "SlaveRoleTypeID", "dbo.RoleType");
            DropForeignKey("dbo.RoleTypeHierarchy", "MasterRoleTypeID", "dbo.RoleType");
            DropForeignKey("dbo.RoleType", "RestrictFactionID", "dbo.Faction");
            DropForeignKey("dbo.PollVote", "OptionID", "dbo.PollOption");
            DropForeignKey("dbo.PollVote", "PollID", "dbo.Poll");
            DropForeignKey("dbo.PollVote", "AccountID", "dbo.Account");
            DropForeignKey("dbo.PollOption", "PollID", "dbo.Poll");
            DropForeignKey("dbo.Poll", "RestrictFactionID", "dbo.Faction");
            DropForeignKey("dbo.Poll", "RestrictClanID", "dbo.Clan");
            DropForeignKey("dbo.Poll", "RoleTargetAccountID", "dbo.Account");
            DropForeignKey("dbo.Poll", "CreatedAccountID", "dbo.Account");
            DropForeignKey("dbo.TreatyEffect", "EffectTypeID", "dbo.TreatyEffectType");
            DropForeignKey("dbo.TreatyEffect", "PlanetID", "dbo.Planet");
            DropForeignKey("dbo.Planet", "MapResourceID", "dbo.Resource");
            DropForeignKey("dbo.PlanetStructure", "StructureTypeID", "dbo.StructureType");
            DropForeignKey("dbo.StructureType", "EffectUnlockID", "dbo.Unlock");
            DropForeignKey("dbo.Unlock", "RequiredUnlockID", "dbo.Unlock");
            DropForeignKey("dbo.KudosPurchase", "UnlockID", "dbo.Unlock");
            DropForeignKey("dbo.KudosPurchase", "AccountID", "dbo.Account");
            DropForeignKey("dbo.CommanderDecoration", "DecorationUnlockID", "dbo.Unlock");
            DropForeignKey("dbo.CommanderDecoration", "SlotID", "dbo.CommanderDecorationSlot");
            DropForeignKey("dbo.CommanderDecoration", "CommanderID", "dbo.Commander");
            DropForeignKey("dbo.Commander", "ChassisUnlockID", "dbo.Unlock");
            DropForeignKey("dbo.CommanderModule", "ModuleUnlockID", "dbo.Unlock");
            DropForeignKey("dbo.CommanderModule", "SlotID", "dbo.CommanderSlot");
            DropForeignKey("dbo.CommanderModule", "CommanderID", "dbo.Commander");
            DropForeignKey("dbo.Commander", "AccountID", "dbo.Account");
            DropForeignKey("dbo.CommanderDecorationIcon", "DecorationUnlockID", "dbo.Unlock");
            DropForeignKey("dbo.AccountUnlock", "UnlockID", "dbo.Unlock");
            DropForeignKey("dbo.AccountUnlock", "AccountID", "dbo.Account");
            DropForeignKey("dbo.PlanetStructure", "TargetPlanetID", "dbo.Planet");
            DropForeignKey("dbo.PlanetStructure", "PlanetID", "dbo.Planet");
            DropForeignKey("dbo.PlanetStructure", "OwnerAccountID", "dbo.Account");
            DropForeignKey("dbo.PlanetOwnerHistory", "PlanetID", "dbo.Planet");
            DropForeignKey("dbo.PlanetOwnerHistory", "OwnerFactionID", "dbo.Faction");
            DropForeignKey("dbo.PlanetOwnerHistory", "OwnerClanID", "dbo.Clan");
            DropForeignKey("dbo.PlanetOwnerHistory", "OwnerAccountID", "dbo.Account");
            DropForeignKey("dbo.PlanetFaction", "PlanetID", "dbo.Planet");
            DropForeignKey("dbo.PlanetFaction", "FactionID", "dbo.Faction");
            DropForeignKey("dbo.MarketOffer", "PlanetID", "dbo.Planet");
            DropForeignKey("dbo.MarketOffer", "AccountID", "dbo.Account");
            DropForeignKey("dbo.MarketOffer", "AcceptedAccountID", "dbo.Account");
            DropForeignKey("dbo.Planet", "GalaxyID", "dbo.Galaxy");
            DropForeignKey("dbo.Link", "PlanetID2", "dbo.Planet");
            DropForeignKey("dbo.Link", "PlanetID1", "dbo.Planet");
            DropForeignKey("dbo.Link", "GalaxyID", "dbo.Galaxy");
            DropForeignKey("dbo.Planet", "ForumThreadID", "dbo.ForumThread");
            DropForeignKey("dbo.News", "ForumThreadID", "dbo.ForumThread");
            DropForeignKey("dbo.News", "AuthorAccountID", "dbo.Account");
            DropForeignKey("dbo.Rating", "MissionID", "dbo.Mission");
            DropForeignKey("dbo.Rating", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Mission", "RatingPollID", "dbo.RatingPoll");
            DropForeignKey("dbo.Mission", "DifficultyRatingPollID", "dbo.RatingPoll");
            DropForeignKey("dbo.SpringBattle", "ModResourceID", "dbo.Resource");
            DropForeignKey("dbo.SpringBattle", "MapResourceID", "dbo.Resource");
            DropForeignKey("dbo.ResourceSpringHash", "ResourceID", "dbo.Resource");
            DropForeignKey("dbo.ResourceDependency", "ResourceID", "dbo.Resource");
            DropForeignKey("dbo.ResourceContentFile", "ResourceID", "dbo.Resource");
            DropForeignKey("dbo.Resource", "RatingPollID", "dbo.RatingPoll");
            DropForeignKey("dbo.Resource", "MissionID", "dbo.Mission");
            DropForeignKey("dbo.MapRating", "ResourceID", "dbo.Resource");
            DropForeignKey("dbo.MapRating", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Resource", "ForumThreadID", "dbo.ForumThread");
            DropForeignKey("dbo.Resource", "TaggedByAccountID", "dbo.Account");
            DropForeignKey("dbo.AccountRatingVote", "RatingPollID", "dbo.RatingPoll");
            DropForeignKey("dbo.AccountRatingVote", "AccountID", "dbo.Account");
            DropForeignKey("dbo.MissionScore", "MissionID", "dbo.Mission");
            DropForeignKey("dbo.MissionScore", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Mission", "Mission1_MissionID", "dbo.Mission");
            DropForeignKey("dbo.Mission", "ForumThreadID", "dbo.ForumThread");
            DropForeignKey("dbo.CampaignPlanet", "MissionID", "dbo.Mission");
            DropForeignKey("dbo.CampaignPlanetVar", new[] { "PlanetID", "CampaignID" }, "dbo.CampaignPlanet");
            DropForeignKey("dbo.CampaignPlanet", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.CampaignLink", new[] { "UnlockingPlanetID", "CampaignID" }, "dbo.CampaignPlanet");
            DropForeignKey("dbo.CampaignLink", new[] { "PlanetToUnlockID", "CampaignID" }, "dbo.CampaignPlanet");
            DropForeignKey("dbo.CampaignLink", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.CampaignJournalVar", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.CampaignEvent", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.CampaignEvent", new[] { "CampaignID", "PlanetID" }, "dbo.CampaignPlanet");
            DropForeignKey("dbo.CampaignEvent", "AccountID", "dbo.Account");
            DropForeignKey("dbo.AccountCampaignVar", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.AccountCampaignProgress", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.AccountCampaignJournalProgress", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.AccountCampaignJournalProgress", new[] { "CampaignID", "JournalID" }, "dbo.CampaignJournal");
            DropForeignKey("dbo.CampaignJournal", new[] { "CampaignID", "PlanetID" }, "dbo.CampaignPlanet");
            DropForeignKey("dbo.CampaignJournalVar", new[] { "CampaignID", "JournalID" }, "dbo.CampaignJournal");
            DropForeignKey("dbo.CampaignJournalVar", new[] { "CampaignID", "RequiredVarID" }, "dbo.CampaignVar");
            DropForeignKey("dbo.CampaignPlanetVar", new[] { "CampaignID", "RequiredVarID" }, "dbo.CampaignVar");
            DropForeignKey("dbo.CampaignPlanetVar", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.CampaignVar", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.AccountCampaignVar", new[] { "CampaignID", "VarID" }, "dbo.CampaignVar");
            DropForeignKey("dbo.AccountCampaignVar", "AccountID", "dbo.Account");
            DropForeignKey("dbo.CampaignJournal", "CampaignID", "dbo.Campaign");
            DropForeignKey("dbo.AccountCampaignJournalProgress", "AccountID", "dbo.Account");
            DropForeignKey("dbo.AccountCampaignProgress", new[] { "CampaignID", "PlanetID" }, "dbo.CampaignPlanet");
            DropForeignKey("dbo.AccountCampaignProgress", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Mission", "AccountID", "dbo.Account");
            DropForeignKey("dbo.ForumThreadLastRead", "ForumThreadID", "dbo.ForumThread");
            DropForeignKey("dbo.ForumThreadLastRead", "AccountID", "dbo.Account");
            DropForeignKey("dbo.ForumPost", "ForumThreadID", "dbo.ForumThread");
            DropForeignKey("dbo.ForumPostEdit", "ForumPostID", "dbo.ForumPost");
            DropForeignKey("dbo.ForumPostEdit", "EditorAccountID", "dbo.Account");
            DropForeignKey("dbo.AccountForumVote", "ForumPostID", "dbo.ForumPost");
            DropForeignKey("dbo.AccountForumVote", "AccountID", "dbo.Account");
            DropForeignKey("dbo.ForumPost", "AuthorAccountID", "dbo.Account");
            DropForeignKey("dbo.ForumThread", "ForumCategoryID", "dbo.ForumCategory");
            DropForeignKey("dbo.ForumLastRead", "ForumCategoryID", "dbo.ForumCategory");
            DropForeignKey("dbo.ForumLastRead", "AccountID", "dbo.Account");
            DropForeignKey("dbo.ForumCategory", "ParentForumCategoryID", "dbo.ForumCategory");
            DropForeignKey("dbo.ForumThread", "RestrictedClanID", "dbo.Clan");
            DropForeignKey("dbo.ForumThread", "LastPostAccountID", "dbo.Account");
            DropForeignKey("dbo.ForumThread", "CreatedAccountID", "dbo.Account");
            DropForeignKey("dbo.Planet", "OwnerFactionID", "dbo.Faction");
            DropForeignKey("dbo.AccountPlanet", "PlanetID", "dbo.Planet");
            DropForeignKey("dbo.AccountPlanet", "AccountID", "dbo.Account");
            DropForeignKey("dbo.Planet", "OwnerAccountID", "dbo.Account");
            DropForeignKey("dbo.TreatyEffect", "FactionTreatyID", "dbo.FactionTreaty");
            DropForeignKey("dbo.TreatyEffect", "ReceivingFactionID", "dbo.Faction");
            DropForeignKey("dbo.TreatyEffect", "GivingFactionID", "dbo.Faction");
            DropForeignKey("dbo.FactionTreaty", "ProposingFactionID", "dbo.Faction");
            DropForeignKey("dbo.FactionTreaty", "AcceptingFactionID", "dbo.Faction");
            DropForeignKey("dbo.FactionTreaty", "ProposingAccountID", "dbo.Account");
            DropForeignKey("dbo.FactionTreaty", "AcceptedAccountID", "dbo.Account");
            DropForeignKey("dbo.AccountRole", "ClanID", "dbo.Clan");
            DropForeignKey("dbo.AccountRole", "AccountID", "dbo.Account");
            DropForeignKey("dbo.SpringBattle", "HostAccountID", "dbo.Account");
            DropForeignKey("dbo.AccountBattleAward", "AccountID", "dbo.Account");
            DropIndex("dbo.EventAccount", new[] { "EventID" });
            DropIndex("dbo.EventAccount", new[] { "AccountID" });
            DropIndex("dbo.EventSpringBattle", new[] { "SpringBattleID" });
            DropIndex("dbo.EventSpringBattle", new[] { "EventID" });
            DropIndex("dbo.EventPlanet", new[] { "PlanetID" });
            DropIndex("dbo.EventPlanet", new[] { "EventID" });
            DropIndex("dbo.EventFaction", new[] { "FactionID" });
            DropIndex("dbo.EventFaction", new[] { "EventID" });
            DropIndex("dbo.EventClan", new[] { "EventID" });
            DropIndex("dbo.EventClan", new[] { "ClanID" });
            DropIndex("dbo.Punishment", new[] { "CreatedAccountID" });
            DropIndex("dbo.Punishment", new[] { "AccountID" });
            DropIndex("dbo.LobbyChannelSubscription", new[] { "AccountID" });
            DropIndex("dbo.Contribution", new[] { "ContributionJarID" });
            DropIndex("dbo.Contribution", new[] { "ManuallyAddedAccountID" });
            DropIndex("dbo.Contribution", new[] { "AccountID" });
            DropIndex("dbo.ContributionJar", new[] { "GuarantorAccountID" });
            DropIndex("dbo.AutoBanSmurfList", new[] { "AccountID" });
            DropIndex("dbo.AccountUserID", new[] { "AccountID" });
            DropIndex("dbo.AccountIP", new[] { "AccountID" });
            DropIndex("dbo.SpringBattlePlayer", new[] { "AccountID" });
            DropIndex("dbo.SpringBattlePlayer", new[] { "SpringBattleID" });
            DropIndex("dbo.RoleTypeHierarchy", new[] { "SlaveRoleTypeID" });
            DropIndex("dbo.RoleTypeHierarchy", new[] { "MasterRoleTypeID" });
            DropIndex("dbo.RoleType", new[] { "RestrictFactionID" });
            DropIndex("dbo.PollVote", new[] { "OptionID" });
            DropIndex("dbo.PollVote", new[] { "PollID" });
            DropIndex("dbo.PollVote", new[] { "AccountID" });
            DropIndex("dbo.PollOption", new[] { "PollID" });
            DropIndex("dbo.Poll", new[] { "CreatedAccountID" });
            DropIndex("dbo.Poll", new[] { "RestrictClanID" });
            DropIndex("dbo.Poll", new[] { "RestrictFactionID" });
            DropIndex("dbo.Poll", new[] { "RoleTargetAccountID" });
            DropIndex("dbo.Poll", new[] { "RoleTypeID" });
            DropIndex("dbo.KudosPurchase", new[] { "UnlockID" });
            DropIndex("dbo.KudosPurchase", new[] { "AccountID" });
            DropIndex("dbo.CommanderModule", new[] { "ModuleUnlockID" });
            DropIndex("dbo.CommanderModule", new[] { "SlotID" });
            DropIndex("dbo.CommanderModule", new[] { "CommanderID" });
            DropIndex("dbo.Commander", new[] { "ChassisUnlockID" });
            DropIndex("dbo.Commander", new[] { "AccountID" });
            DropIndex("dbo.CommanderDecoration", new[] { "DecorationUnlockID" });
            DropIndex("dbo.CommanderDecoration", new[] { "SlotID" });
            DropIndex("dbo.CommanderDecoration", new[] { "CommanderID" });
            DropIndex("dbo.CommanderDecorationIcon", new[] { "DecorationUnlockID" });
            DropIndex("dbo.AccountUnlock", new[] { "UnlockID" });
            DropIndex("dbo.AccountUnlock", new[] { "AccountID" });
            DropIndex("dbo.Unlock", new[] { "RequiredUnlockID" });
            DropIndex("dbo.StructureType", new[] { "EffectUnlockID" });
            DropIndex("dbo.PlanetStructure", new[] { "TargetPlanetID" });
            DropIndex("dbo.PlanetStructure", new[] { "OwnerAccountID" });
            DropIndex("dbo.PlanetStructure", new[] { "StructureTypeID" });
            DropIndex("dbo.PlanetStructure", new[] { "PlanetID" });
            DropIndex("dbo.PlanetOwnerHistory", new[] { "OwnerFactionID" });
            DropIndex("dbo.PlanetOwnerHistory", new[] { "OwnerClanID" });
            DropIndex("dbo.PlanetOwnerHistory", new[] { "OwnerAccountID" });
            DropIndex("dbo.PlanetOwnerHistory", new[] { "PlanetID" });
            DropIndex("dbo.PlanetFaction", new[] { "FactionID" });
            DropIndex("dbo.PlanetFaction", new[] { "PlanetID" });
            DropIndex("dbo.MarketOffer", new[] { "AcceptedAccountID" });
            DropIndex("dbo.MarketOffer", new[] { "PlanetID" });
            DropIndex("dbo.MarketOffer", new[] { "AccountID" });
            DropIndex("dbo.Link", new[] { "GalaxyID" });
            DropIndex("dbo.Link", new[] { "PlanetID2" });
            DropIndex("dbo.Link", new[] { "PlanetID1" });
            DropIndex("dbo.News", new[] { "ForumThreadID" });
            DropIndex("dbo.News", new[] { "AuthorAccountID" });
            DropIndex("dbo.Rating", new[] { "MissionID" });
            DropIndex("dbo.Rating", new[] { "AccountID" });
            DropIndex("dbo.ResourceSpringHash", new[] { "ResourceID" });
            DropIndex("dbo.ResourceDependency", new[] { "ResourceID" });
            DropIndex("dbo.ResourceContentFile", new[] { "ResourceID" });
            DropIndex("dbo.MapRating", new[] { "AccountID" });
            DropIndex("dbo.MapRating", new[] { "ResourceID" });
            DropIndex("dbo.Resource", new[] { "RatingPollID" });
            DropIndex("dbo.Resource", new[] { "ForumThreadID" });
            DropIndex("dbo.Resource", new[] { "TaggedByAccountID" });
            DropIndex("dbo.Resource", new[] { "MissionID" });
            DropIndex("dbo.AccountRatingVote", new[] { "AccountID" });
            DropIndex("dbo.AccountRatingVote", new[] { "RatingPollID" });
            DropIndex("dbo.MissionScore", new[] { "AccountID" });
            DropIndex("dbo.MissionScore", new[] { "MissionID" });
            DropIndex("dbo.CampaignLink", new[] { "CampaignID" });
            DropIndex("dbo.CampaignLink", new[] { "UnlockingPlanetID", "CampaignID" });
            DropIndex("dbo.CampaignLink", new[] { "PlanetToUnlockID", "CampaignID" });
            DropIndex("dbo.CampaignEvent", new[] { "CampaignID", "PlanetID" });
            DropIndex("dbo.CampaignEvent", new[] { "AccountID" });
            DropIndex("dbo.CampaignPlanetVar", new[] { "PlanetID", "CampaignID" });
            DropIndex("dbo.CampaignPlanetVar", new[] { "CampaignID", "RequiredVarID" });
            DropIndex("dbo.CampaignPlanetVar", new[] { "CampaignID" });
            DropIndex("dbo.AccountCampaignVar", new[] { "CampaignID", "VarID" });
            DropIndex("dbo.AccountCampaignVar", new[] { "AccountID" });
            DropIndex("dbo.CampaignVar", new[] { "CampaignID" });
            DropIndex("dbo.CampaignJournalVar", new[] { "CampaignID", "JournalID" });
            DropIndex("dbo.CampaignJournalVar", new[] { "CampaignID", "RequiredVarID" });
            DropIndex("dbo.CampaignJournal", new[] { "CampaignID", "PlanetID" });
            DropIndex("dbo.CampaignJournal", new[] { "CampaignID" });
            DropIndex("dbo.AccountCampaignJournalProgress", new[] { "CampaignID", "JournalID" });
            DropIndex("dbo.AccountCampaignJournalProgress", new[] { "AccountID" });
            DropIndex("dbo.AccountCampaignProgress", new[] { "CampaignID", "PlanetID" });
            DropIndex("dbo.AccountCampaignProgress", new[] { "AccountID" });
            DropIndex("dbo.CampaignPlanet", new[] { "MissionID" });
            DropIndex("dbo.CampaignPlanet", new[] { "CampaignID" });
            DropIndex("dbo.Mission", new[] { "Mission1_MissionID" });
            DropIndex("dbo.Mission", new[] { "DifficultyRatingPollID" });
            DropIndex("dbo.Mission", new[] { "RatingPollID" });
            DropIndex("dbo.Mission", new[] { "ForumThreadID" });
            DropIndex("dbo.Mission", new[] { "AccountID" });
            DropIndex("dbo.ForumThreadLastRead", new[] { "AccountID" });
            DropIndex("dbo.ForumThreadLastRead", new[] { "ForumThreadID" });
            DropIndex("dbo.ForumPostEdit", new[] { "EditorAccountID" });
            DropIndex("dbo.ForumPostEdit", new[] { "ForumPostID" });
            DropIndex("dbo.AccountForumVote", new[] { "ForumPostID" });
            DropIndex("dbo.AccountForumVote", new[] { "AccountID" });
            DropIndex("dbo.ForumPost", new[] { "ForumThreadID" });
            DropIndex("dbo.ForumPost", new[] { "AuthorAccountID" });
            DropIndex("dbo.ForumLastRead", new[] { "ForumCategoryID" });
            DropIndex("dbo.ForumLastRead", new[] { "AccountID" });
            DropIndex("dbo.ForumCategory", new[] { "ParentForumCategoryID" });
            DropIndex("dbo.ForumThread", new[] { "RestrictedClanID" });
            DropIndex("dbo.ForumThread", new[] { "ForumCategoryID" });
            DropIndex("dbo.ForumThread", new[] { "LastPostAccountID" });
            DropIndex("dbo.ForumThread", new[] { "CreatedAccountID" });
            DropIndex("dbo.AccountPlanet", new[] { "AccountID" });
            DropIndex("dbo.AccountPlanet", new[] { "PlanetID" });
            DropIndex("dbo.Planet", new[] { "OwnerFactionID" });
            DropIndex("dbo.Planet", new[] { "ForumThreadID" });
            DropIndex("dbo.Planet", new[] { "GalaxyID" });
            DropIndex("dbo.Planet", new[] { "OwnerAccountID" });
            DropIndex("dbo.Planet", new[] { "MapResourceID" });
            DropIndex("dbo.TreatyEffect", new[] { "PlanetID" });
            DropIndex("dbo.TreatyEffect", new[] { "ReceivingFactionID" });
            DropIndex("dbo.TreatyEffect", new[] { "GivingFactionID" });
            DropIndex("dbo.TreatyEffect", new[] { "EffectTypeID" });
            DropIndex("dbo.TreatyEffect", new[] { "FactionTreatyID" });
            DropIndex("dbo.FactionTreaty", new[] { "AcceptedAccountID" });
            DropIndex("dbo.FactionTreaty", new[] { "AcceptingFactionID" });
            DropIndex("dbo.FactionTreaty", new[] { "ProposingAccountID" });
            DropIndex("dbo.FactionTreaty", new[] { "ProposingFactionID" });
            DropIndex("dbo.AccountRole", new[] { "ClanID" });
            DropIndex("dbo.AccountRole", new[] { "FactionID" });
            DropIndex("dbo.AccountRole", new[] { "RoleTypeID" });
            DropIndex("dbo.AccountRole", new[] { "AccountID" });
            DropIndex("dbo.Clan", new[] { "FactionID" });
            DropIndex("dbo.Clan", new[] { "ForumThreadID" });
            DropIndex("dbo.SpringBattle", new[] { "RatingPollID" });
            DropIndex("dbo.SpringBattle", new[] { "ForumThreadID" });
            DropIndex("dbo.SpringBattle", new[] { "ModResourceID" });
            DropIndex("dbo.SpringBattle", new[] { "MapResourceID" });
            DropIndex("dbo.SpringBattle", new[] { "HostAccountID" });
            DropIndex("dbo.AccountBattleAward", new[] { "SpringBattleID" });
            DropIndex("dbo.AccountBattleAward", new[] { "AccountID" });
            DropIndex("dbo.Account", new[] { "FactionID" });
            DropIndex("dbo.Account", new[] { "ClanID" });
            DropIndex("dbo.AbuseReport", new[] { "ReporterAccountID" });
            DropIndex("dbo.AbuseReport", new[] { "AccountID" });
            DropTable("dbo.EventAccount");
            DropTable("dbo.EventSpringBattle");
            DropTable("dbo.EventPlanet");
            DropTable("dbo.EventFaction");
            DropTable("dbo.EventClan");
            DropTable("dbo.MiscVar");
            DropTable("dbo.LobbyMessage");
            DropTable("dbo.ExceptionLog");
            DropTable("dbo.BlockedHost");
            DropTable("dbo.BlockedCompany");
            DropTable("dbo.Avatar");
            DropTable("dbo.AutohostConfig");
            DropTable("dbo.Punishment");
            DropTable("dbo.LobbyChannelSubscription");
            DropTable("dbo.Contribution");
            DropTable("dbo.ContributionJar");
            DropTable("dbo.AutoBanSmurfList");
            DropTable("dbo.AccountUserID");
            DropTable("dbo.AccountIP");
            DropTable("dbo.SpringBattlePlayer");
            DropTable("dbo.RoleTypeHierarchy");
            DropTable("dbo.RoleType");
            DropTable("dbo.PollVote");
            DropTable("dbo.PollOption");
            DropTable("dbo.Poll");
            DropTable("dbo.TreatyEffectType");
            DropTable("dbo.KudosPurchase");
            DropTable("dbo.CommanderDecorationSlot");
            DropTable("dbo.CommanderSlot");
            DropTable("dbo.CommanderModule");
            DropTable("dbo.Commander");
            DropTable("dbo.CommanderDecoration");
            DropTable("dbo.CommanderDecorationIcon");
            DropTable("dbo.AccountUnlock");
            DropTable("dbo.Unlock");
            DropTable("dbo.StructureType");
            DropTable("dbo.PlanetStructure");
            DropTable("dbo.PlanetOwnerHistory");
            DropTable("dbo.PlanetFaction");
            DropTable("dbo.MarketOffer");
            DropTable("dbo.Link");
            DropTable("dbo.Galaxy");
            DropTable("dbo.News");
            DropTable("dbo.Rating");
            DropTable("dbo.ResourceSpringHash");
            DropTable("dbo.ResourceDependency");
            DropTable("dbo.ResourceContentFile");
            DropTable("dbo.MapRating");
            DropTable("dbo.Resource");
            DropTable("dbo.AccountRatingVote");
            DropTable("dbo.RatingPoll");
            DropTable("dbo.MissionScore");
            DropTable("dbo.CampaignLink");
            DropTable("dbo.CampaignEvent");
            DropTable("dbo.CampaignPlanetVar");
            DropTable("dbo.AccountCampaignVar");
            DropTable("dbo.CampaignVar");
            DropTable("dbo.CampaignJournalVar");
            DropTable("dbo.CampaignJournal");
            DropTable("dbo.AccountCampaignJournalProgress");
            DropTable("dbo.Campaign");
            DropTable("dbo.AccountCampaignProgress");
            DropTable("dbo.CampaignPlanet");
            DropTable("dbo.Mission");
            DropTable("dbo.ForumThreadLastRead");
            DropTable("dbo.ForumPostEdit");
            DropTable("dbo.AccountForumVote");
            DropTable("dbo.ForumPost");
            DropTable("dbo.ForumLastRead");
            DropTable("dbo.ForumCategory");
            DropTable("dbo.ForumThread");
            DropTable("dbo.AccountPlanet");
            DropTable("dbo.Planet");
            DropTable("dbo.TreatyEffect");
            DropTable("dbo.FactionTreaty");
            DropTable("dbo.Faction");
            DropTable("dbo.AccountRole");
            DropTable("dbo.Clan");
            DropTable("dbo.Event");
            DropTable("dbo.SpringBattle");
            DropTable("dbo.AccountBattleAward");
            DropTable("dbo.Account");
            DropTable("dbo.AbuseReport");
        }
    }
}
