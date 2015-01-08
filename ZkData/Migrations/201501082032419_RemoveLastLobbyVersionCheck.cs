namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveLastLobbyVersionCheck : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "LastLobbyVersionCheck");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "LastLobbyVersionCheck", c => c.DateTime());
        }
    }
}
