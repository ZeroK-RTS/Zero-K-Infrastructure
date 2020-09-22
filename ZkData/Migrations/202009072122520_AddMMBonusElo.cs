namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMMBonusElo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MmEloBonusMultiplier", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MmEloBonusMultiplier");
        }
    }
}
