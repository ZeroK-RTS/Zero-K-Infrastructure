namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMmLadderDecay : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MmLadderDecayPerHour", c => c.Double(nullable: false, defaultValue: 1.0));
            AddColumn("dbo.DynamicConfigs", "MmLadderMaxDecay", c => c.Int(nullable: false, defaultValue: 500));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MmLadderMaxDecay");
            DropColumn("dbo.DynamicConfigs", "MmLadderDecayPerHour");
        }
    }
}
