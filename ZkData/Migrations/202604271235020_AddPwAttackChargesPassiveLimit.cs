namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPwAttackChargesPassiveLimit : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "PwAttackChargesPassiveLimit", c => c.Int(nullable: false, defaultValue: 1));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "PwAttackChargesPassiveLimit");
        }
    }
}
