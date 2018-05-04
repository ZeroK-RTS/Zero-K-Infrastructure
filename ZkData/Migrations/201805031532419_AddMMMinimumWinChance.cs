namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMMMinimumWinChance : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MmMinimumWinChance", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MmMinimumWinChance");
        }
    }
}
