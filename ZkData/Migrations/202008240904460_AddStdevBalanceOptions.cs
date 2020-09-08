namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddStdevBalanceOptions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MinimumPlayersForStdevBalance", c => c.Int(nullable: false));
            AddColumn("dbo.DynamicConfigs", "StdevBalanceWeight", c => c.Double(nullable: false));
            DropColumn("dbo.DynamicConfigs", "MinimumDynamicMaxLadderEloStdev");
        }
        
        public override void Down()
        {
            AddColumn("dbo.DynamicConfigs", "MinimumDynamicMaxLadderEloStdev", c => c.Single(nullable: false));
            DropColumn("dbo.DynamicConfigs", "StdevBalanceWeight");
            DropColumn("dbo.DynamicConfigs", "MinimumPlayersForStdevBalance");
        }
    }
}
