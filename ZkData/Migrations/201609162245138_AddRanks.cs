namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRanks : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "CasualRank", c => c.Int());
            AddColumn("dbo.Accounts", "CompetitiveRank", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "CompetitiveRank");
            DropColumn("dbo.Accounts", "CasualRank");
        }
    }
}
