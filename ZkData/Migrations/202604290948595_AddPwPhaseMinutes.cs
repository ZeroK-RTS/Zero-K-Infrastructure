namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPwPhaseMinutes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "PwAttackPhaseMinutes", c => c.Int(nullable: false, defaultValue: 2));
            AddColumn("dbo.DynamicConfigs", "PwDefendPhaseMinutes", c => c.Int(nullable: false, defaultValue: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "PwDefendPhaseMinutes");
            DropColumn("dbo.DynamicConfigs", "PwAttackPhaseMinutes");
        }
    }
}
