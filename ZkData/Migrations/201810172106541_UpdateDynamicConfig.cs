namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateDynamicConfig : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MmMinimumMinutesBetweenGames", c => c.Double(nullable: false));
            AddColumn("dbo.DynamicConfigs", "MmMinimumMinutesBetweenSuggestions", c => c.Double(nullable: false));
            AddColumn("dbo.DynamicConfigs", "MaximumBattlePlayers", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MaximumBattlePlayers");
            DropColumn("dbo.DynamicConfigs", "MmMinimumMinutesBetweenSuggestions");
            DropColumn("dbo.DynamicConfigs", "MmMinimumMinutesBetweenGames");
        }
    }
}
