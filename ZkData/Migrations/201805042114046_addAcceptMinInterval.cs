namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addAcceptMinInterval : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "AcceptMinInterval", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "AcceptMinInterval");
        }
    }
}
