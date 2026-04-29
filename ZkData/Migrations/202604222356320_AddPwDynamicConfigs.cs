namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPwDynamicConfigs : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "PwAttackOptionCount", c => c.Int(nullable: false, defaultValue: 6));
            AddColumn("dbo.DynamicConfigs", "PwAttackChargesMax", c => c.Int(nullable: false, defaultValue: 2));
            AddColumn("dbo.DynamicConfigs", "PwAttackChargesRechargeMinutes", c => c.Int(nullable: false, defaultValue: 60));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "PwAttackChargesRechargeMinutes");
            DropColumn("dbo.DynamicConfigs", "PwAttackChargesMax");
            DropColumn("dbo.DynamicConfigs", "PwAttackOptionCount");
        }
    }
}
