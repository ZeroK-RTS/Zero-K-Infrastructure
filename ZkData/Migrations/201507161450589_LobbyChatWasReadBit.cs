namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LobbyChatWasReadBit : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LobbyChatHistories", "WasRead", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.LobbyChatHistories", "WasRead");
        }
    }
}
