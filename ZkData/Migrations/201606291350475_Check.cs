namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Check : DbMigration
    {
        public override void Up()
        {
            /*DropColumn("dbo.CommanderSlots", "ChassisID");
            RenameColumn(table: "dbo.CommanderSlots", name: "Chassis_UnlockID", newName: "ChassisID");
            RenameIndex(table: "dbo.CommanderSlots", name: "IX_Chassis_UnlockID", newName: "IX_ChassisID");*/
        }
        
        public override void Down()
        {
            /*RenameIndex(table: "dbo.CommanderSlots", name: "IX_ChassisID", newName: "IX_Chassis_UnlockID");
            RenameColumn(table: "dbo.CommanderSlots", name: "ChassisID", newName: "Chassis_UnlockID");
            AddColumn("dbo.CommanderSlots", "ChassisID", c => c.Int());*/
        }
    }
}
