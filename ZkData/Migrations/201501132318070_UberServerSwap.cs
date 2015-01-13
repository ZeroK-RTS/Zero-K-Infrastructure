namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UberServerSwap : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Accounts", new[] { "LobbyID" });
            AddColumn("dbo.Accounts", "Cpu", c => c.Int(nullable: false));
            DropColumn("dbo.Accounts", "LobbyTimeRank");
            DropColumn("dbo.Accounts", "LastLobbyVersionCheck");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "LastLobbyVersionCheck", c => c.DateTime());
            AddColumn("dbo.Accounts", "LobbyTimeRank", c => c.Int(nullable: false));
            DropColumn("dbo.Accounts", "Cpu");
            CreateIndex("dbo.Accounts", "LobbyID");
        }
    }
}
