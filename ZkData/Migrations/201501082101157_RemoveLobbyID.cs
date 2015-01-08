namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveLobbyID : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Accounts", new[] { "LobbyID" });
            DropColumn("dbo.Accounts", "LobbyID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "LobbyID", c => c.Int());
            CreateIndex("dbo.Accounts", "LobbyID");
        }
    }
}
