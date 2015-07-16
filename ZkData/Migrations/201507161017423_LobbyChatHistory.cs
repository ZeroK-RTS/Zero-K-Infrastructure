namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LobbyChatHistory : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.LobbyChannelSubscriptions", "AccountID", "dbo.Accounts");
            DropIndex("dbo.LobbyChannelSubscriptions", new[] { "AccountID" });
            CreateTable(
                "dbo.LobbyChatHistories",
                c => new
                    {
                        LobbyChatHistoryID = c.Int(nullable: false, identity: true),
                        Ring = c.Boolean(nullable: false),
                        SayPlace = c.Int(nullable: false),
                        Target = c.String(maxLength: 255),
                        Text = c.String(),
                        Time = c.DateTime(nullable: false),
                        User = c.String(maxLength: 255),
                        IsEmote = c.Boolean(nullable: false),
                        UserAccountID = c.Int(),
                        TargetAccountID = c.Int(),
                        TargetAccount_AccountID = c.Int(),
                        UserAccount_AccountID = c.Int(),
                    })
                .PrimaryKey(t => t.LobbyChatHistoryID)
                .ForeignKey("dbo.Accounts", t => t.TargetAccount_AccountID)
                .ForeignKey("dbo.Accounts", t => t.UserAccount_AccountID)
                .Index(t => t.Target)
                .Index(t => t.Time, clustered: true)
                .Index(t => t.User)
                .Index(t => t.TargetAccount_AccountID)
                .Index(t => t.UserAccount_AccountID);
            
            DropTable("dbo.LobbyChannelSubscriptions");
            DropTable("dbo.LobbyMessages");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.LobbyMessages",
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
                "dbo.LobbyChannelSubscriptions",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        Channel = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => new { t.AccountID, t.Channel });
            
            DropForeignKey("dbo.LobbyChatHistories", "UserAccount_AccountID", "dbo.Accounts");
            DropForeignKey("dbo.LobbyChatHistories", "TargetAccount_AccountID", "dbo.Accounts");
            DropIndex("dbo.LobbyChatHistories", new[] { "UserAccount_AccountID" });
            DropIndex("dbo.LobbyChatHistories", new[] { "TargetAccount_AccountID" });
            DropIndex("dbo.LobbyChatHistories", new[] { "User" });
            DropIndex("dbo.LobbyChatHistories", new[] { "Time" });
            DropIndex("dbo.LobbyChatHistories", new[] { "Target" });
            DropTable("dbo.LobbyChatHistories");
            CreateIndex("dbo.LobbyChannelSubscriptions", "AccountID");
            AddForeignKey("dbo.LobbyChannelSubscriptions", "AccountID", "dbo.Accounts", "AccountID", cascadeDelete: true);
        }
    }
}
