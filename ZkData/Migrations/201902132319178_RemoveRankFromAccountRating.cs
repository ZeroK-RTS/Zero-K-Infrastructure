namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveRankFromAccountRating : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.AccountRatings", new[] { "Rank" });
            AddColumn("dbo.AccountRatings", "IsRanked", c => c.Boolean(nullable: false));
            CreateIndex("dbo.AccountRatings", "IsRanked");
            DropColumn("dbo.AccountRatings", "Rank");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AccountRatings", "Rank", c => c.Int(nullable: false));
            DropIndex("dbo.AccountRatings", new[] { "IsRanked" });
            DropColumn("dbo.AccountRatings", "IsRanked");
            CreateIndex("dbo.AccountRatings", "Rank");
        }
    }
}
