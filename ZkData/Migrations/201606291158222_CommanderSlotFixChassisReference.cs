namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CommanderSlotFixChassisReference : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CommanderSlots", "ChassisID", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.CommanderSlots", "ChassisID");
        }
    }
}
