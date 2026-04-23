namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPwAttackCharges : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "PwAttackCharges", c => c.Int(nullable: false, defaultValue: 2));
            AddColumn("dbo.Accounts", "PwLastAttackTurn", c => c.Int());
            AddColumn("dbo.DynamicConfigs", "PwAttackChargesCooldownTurns", c => c.Int(nullable: false, defaultValue: 3));
            DropColumn("dbo.DynamicConfigs", "PwAttackChargesRechargeMinutes");
        }

        public override void Down()
        {
            AddColumn("dbo.DynamicConfigs", "PwAttackChargesRechargeMinutes", c => c.Int(nullable: false, defaultValue: 60));
            DropColumn("dbo.DynamicConfigs", "PwAttackChargesCooldownTurns");
            DropColumn("dbo.Accounts", "PwLastAttackTurn");
            DropColumn("dbo.Accounts", "PwAttackCharges");
        }
    }
}
