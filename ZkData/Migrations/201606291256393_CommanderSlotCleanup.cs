namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CommanderSlotCleanup : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CommanderSlots", "Chassis_UnlockID", "dbo.Unlocks");
            DropIndex("dbo.CommanderSlots", new[] { "Chassis_UnlockID" });
            DropColumn("dbo.CommanderSlots", "Chassis_UnlockID");

            AddForeignKey("dbo.CommanderSlots", "ChassisID", "dbo.Unlocks", "UnlockID");
        }

        public override void Down()
        {
            AddColumn("dbo.CommanderSlots", "Chassis_UnlockID", c => c.Int());
            CreateIndex("dbo.CommanderSlots", "Chassis_UnlockID");
            AddForeignKey("dbo.CommanderSlots", "Chassis_UnlockID", "dbo.Unlocks", "UnlockID");

            DropForeignKey("dbo.CommanderSlots", "ChassisID", "dbo.Unlocks");
        }
    }
}
