namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMaxEloStDevToDynConfing : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MinimumDynamicMaxLadderEloStdev", c => c.Single(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MinimumDynamicMaxLadderEloStdev");
        }
    }
}
