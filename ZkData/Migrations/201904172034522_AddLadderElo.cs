namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLadderElo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AccountRatings", "LadderElo", c => c.Double());
            CreateIndex("dbo.AccountRatings", "LadderElo");
            Sql("Update dbo.AccountRatings Set LadderElo = RealElo");
        }
        
        public override void Down()
        {
            DropIndex("dbo.AccountRatings", new[] { "LadderElo" });
            DropColumn("dbo.AccountRatings", "LadderElo");
        }
    }
}
