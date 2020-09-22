namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TimeQueueSwitch : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "TimeQueueEnabled", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "TimeQueueEnabled");
        }
    }
}
