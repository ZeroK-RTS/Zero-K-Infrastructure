namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StoreSourceInChatHistory : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LobbyChatHistories", "Source", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.LobbyChatHistories", "Source");
        }
    }
}
