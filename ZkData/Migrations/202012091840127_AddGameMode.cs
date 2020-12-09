namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGameMode : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GameModes",
                c => new
                    {
                        GameModeID = c.Int(nullable: false, identity: true),
                        IsFeatured = c.Boolean(nullable: false),
                        ShortName = c.String(maxLength: 64),
                        DisplayName = c.String(),
                        Created = c.DateTime(nullable: false),
                        LastModified = c.DateTime(nullable: false),
                        ForumThreadID = c.Int(),
                        MaintainerAccountID = c.Int(nullable: false),
                        GameModeJson = c.String(),
                    })
                .PrimaryKey(t => t.GameModeID)
                .ForeignKey("dbo.Accounts", t => t.MaintainerAccountID, cascadeDelete: true)
                .ForeignKey("dbo.ForumThreads", t => t.ForumThreadID)
                .Index(t => t.ShortName)
                .Index(t => t.ForumThreadID)
                .Index(t => t.MaintainerAccountID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.GameModes", "ForumThreadID", "dbo.ForumThreads");
            DropForeignKey("dbo.GameModes", "MaintainerAccountID", "dbo.Accounts");
            DropIndex("dbo.GameModes", new[] { "MaintainerAccountID" });
            DropIndex("dbo.GameModes", new[] { "ForumThreadID" });
            DropIndex("dbo.GameModes", new[] { "ShortName" });
            DropTable("dbo.GameModes");
        }
    }
}
