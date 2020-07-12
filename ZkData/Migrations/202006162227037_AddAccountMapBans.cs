namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAccountMapBans : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AccountMapBans",
                c => new
                {
                    AccountID = c.Int(nullable: false),
                    BannedMapResourceID = c.Int(nullable: false),
                    Rank = c.Int(nullable: false),
                })
                .PrimaryKey(t => new { t.AccountID, t.BannedMapResourceID, t.Rank })
                .ForeignKey("dbo.Resources", t => t.BannedMapResourceID)
                .ForeignKey("dbo.Accounts", t => t.AccountID)
                .Index(t => t.AccountID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AccountMapBans", "AccountID", "dbo.Accounts");
            DropForeignKey("dbo.AccountMapBans", "BannedMapResourceID", "dbo.Resources");
            DropIndex("dbo.AccountMapBans", new[] { "AccountID" });
            DropTable("dbo.AccountMapBans");
        }
    }
}
