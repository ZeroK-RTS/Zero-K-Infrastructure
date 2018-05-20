namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MmSizeGrowthTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MmSizeGrowthTime", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MmSizeGrowthTime");
        }
    }
}
