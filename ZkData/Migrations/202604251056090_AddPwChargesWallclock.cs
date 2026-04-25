namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddPwChargesWallclock : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "PwLastChargeChangeTurn");
            AddColumn("dbo.Accounts", "PwLastChargeChange", c => c.DateTime());
            DropColumn("dbo.DynamicConfigs", "PwAttackChargesCooldownTurns");
            AddColumn("dbo.DynamicConfigs", "PwAttackChargesCooldownMinutes", c => c.Int(nullable: false, defaultValue: 60));
        }

        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "PwAttackChargesCooldownMinutes");
            AddColumn("dbo.DynamicConfigs", "PwAttackChargesCooldownTurns", c => c.Int(nullable: false, defaultValue: 4));
            DropColumn("dbo.Accounts", "PwLastChargeChange");
            AddColumn("dbo.Accounts", "PwLastChargeChangeTurn", c => c.Int());
        }
    }
}