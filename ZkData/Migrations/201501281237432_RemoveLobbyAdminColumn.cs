namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveLobbyAdminColumn : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "IsLobbyAdministrator");
            DropColumn("dbo.SpringBattlePlayers", "Rank");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SpringBattlePlayers", "Rank", c => c.Int(nullable: false));
            AddColumn("dbo.Accounts", "IsLobbyAdministrator", c => c.Boolean(nullable: false));
        }
    }
}
