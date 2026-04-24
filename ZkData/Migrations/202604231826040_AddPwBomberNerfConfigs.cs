namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPwBomberNerfConfigs : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "PwBomberSelfIpRate", c => c.Double(nullable: false, defaultValue: 0.5));
            AddColumn("dbo.DynamicConfigs", "PwBomberMinimumIpFloor", c => c.Double(nullable: false, defaultValue: 5.0));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "PwBomberMinimumIpFloor");
            DropColumn("dbo.DynamicConfigs", "PwBomberSelfIpRate");
        }
    }
}
