namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EloIsHistory : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "Elo");
            DropColumn("dbo.Accounts", "EloWeight");
            DropColumn("dbo.Accounts", "EloMm");
            DropColumn("dbo.Accounts", "EloMmWeight");
            DropColumn("dbo.Accounts", "EloPw");
            DropColumn("dbo.Accounts", "CasualRank");
            DropColumn("dbo.Accounts", "CompetitiveRank");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "CompetitiveRank", c => c.Int());
            AddColumn("dbo.Accounts", "CasualRank", c => c.Int());
            AddColumn("dbo.Accounts", "EloPw", c => c.Double(nullable: false));
            AddColumn("dbo.Accounts", "EloMmWeight", c => c.Double(nullable: false));
            AddColumn("dbo.Accounts", "EloMm", c => c.Double(nullable: false));
            AddColumn("dbo.Accounts", "EloWeight", c => c.Double(nullable: false));
            AddColumn("dbo.Accounts", "Elo", c => c.Double(nullable: false));
        }
    }
}
