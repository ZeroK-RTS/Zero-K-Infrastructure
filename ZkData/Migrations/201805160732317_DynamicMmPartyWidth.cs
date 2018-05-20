namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DynamicMmPartyWidth : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MmWidthReductionForParties", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MmWidthReductionForParties");
        }
    }
}
