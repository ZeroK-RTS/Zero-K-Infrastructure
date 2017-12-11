namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAccountRatings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AccountRatings",
                c => new
                    {
                        AccountID = c.Int(nullable: false),
                        RatingCategory = c.Int(nullable: false),
                        Percentile = c.Double(nullable: false),
                        Rank = c.Int(nullable: false),
                        RealElo = c.Double(nullable: false),
                        LastUncertainty = c.Double(nullable: false),
                        LastGameDate = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.AccountID, t.RatingCategory })
                .ForeignKey("dbo.Accounts", t => t.AccountID, cascadeDelete: true)
                .Index(t => t.AccountID)
                .Index(t => t.Rank)
                .Index(t => t.RealElo);
            
            AddColumn("dbo.SpringBattles", "ApplicableRatings", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AccountRatings", "AccountID", "dbo.Accounts");
            DropIndex("dbo.AccountRatings", new[] { "RealElo" });
            DropIndex("dbo.AccountRatings", new[] { "Rank" });
            DropIndex("dbo.AccountRatings", new[] { "AccountID" });
            DropColumn("dbo.SpringBattles", "ApplicableRatings");
            DropTable("dbo.AccountRatings");
        }
    }
}
