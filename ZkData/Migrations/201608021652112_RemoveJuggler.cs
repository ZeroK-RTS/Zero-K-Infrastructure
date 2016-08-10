namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveJuggler : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.AutohostConfigs", "MinToJuggle");
            DropColumn("dbo.AutohostConfigs", "MaxToJuggle");
            DropColumn("dbo.AutohostConfigs", "SplitBiggerThan");
            DropColumn("dbo.AutohostConfigs", "MergeSmallerThan");
            DropColumn("dbo.AutohostConfigs", "DontMoveManuallyJoined");
            DropColumn("dbo.AutohostConfigs", "IsTrollHost");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AutohostConfigs", "IsTrollHost", c => c.Boolean());
            AddColumn("dbo.AutohostConfigs", "DontMoveManuallyJoined", c => c.Boolean());
            AddColumn("dbo.AutohostConfigs", "MergeSmallerThan", c => c.Int());
            AddColumn("dbo.AutohostConfigs", "SplitBiggerThan", c => c.Int());
            AddColumn("dbo.AutohostConfigs", "MaxToJuggle", c => c.Int());
            AddColumn("dbo.AutohostConfigs", "MinToJuggle", c => c.Int());
        }
    }
}
