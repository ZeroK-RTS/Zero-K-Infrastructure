namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MmTeamsMinimumWinChance : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MmTeamsMinimumWinChance", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MmTeamsMinimumWinChance");
        }
    }
}
