namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPinnedOrderForLobbyNews : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LobbyNews", "PinnedOrder", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.LobbyNews", "PinnedOrder");
        }
    }
}
